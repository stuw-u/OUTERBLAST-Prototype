using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System;
using System.Collections.Generic;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using Blast.Collections;

// DELETE ME

// My WORST ennemy
public struct InputPacket {
    public int packetIndex;

    public int startInputIndex;
    public int inputLenght;

    public int nextInputPacketIndex;
    public int savedStateIndex;

    public InputPacket (int packetIndex, int startInputIndex, int inputLenght) : this() {
        this.packetIndex = packetIndex;
        this.startInputIndex = startInputIndex;
        this.inputLenght = inputLenght;
    }

    public InputPacket (int packetIndex, int startInputIndex, int inputLenght, int nextInputPacketIndex, int savedStateIndex) {
        this.packetIndex = packetIndex;
        this.startInputIndex = startInputIndex;
        this.inputLenght = inputLenght;
        this.nextInputPacketIndex = nextInputPacketIndex;
        this.savedStateIndex = savedStateIndex;
    }
}



public class CorrectionMessage : AutoNetworkSerializable {
    public int nextInputPacketIndex;
    public int savedStateIndex;
    public Vector3 position;
    public Vector3 velocity;
    public Quaternion rotation;

    public CorrectionMessage (int nextInputPacketIndex, int savedStateIndex, Vector3 position, Vector3 velocity, Quaternion rotation) {
        this.nextInputPacketIndex = nextInputPacketIndex;
        this.savedStateIndex = savedStateIndex;
        this.position = position;
        this.velocity = velocity;
        this.rotation = rotation;
    }

    public CorrectionMessage () {
    }
}


public class InputSnapshotRecorder {

    // Should be able to check if inputs have been recorded from a specific packetTime
    // Should have a function to replay inputs up until now.
    // Should include an input enqueue function that discard old inputs.

    private LimitedQueue<InputSnapshot> recordQueue;
    private List<InputPacket> packets;
    private int packetIndex = 0;
    private int minPacketIndexInList = 0;

    public int inputIndex = 0;
    public InputSnapshotRecorder (int maxInputs) {
        recordQueue = new LimitedQueue<InputSnapshot>(maxInputs);
        packets = new List<InputPacket>(8);
        packets.Add(new InputPacket(0, 0, 0));
        packetIndex = 1;
        inputIndex = 0;
    }


    public void WriteLastInputsToStream (NetworkWriter writer, int savedStateIndex) {
        InputPacket lastest = packets[packets.Count - 1];
        
        // Write more that the inputs of the input lenght in case of packet loss. The server will figure how to use the data sent

        int inputsToInclude = math.min(recordQueue.Count, lastest.inputLenght + InputBufferController.includedInputCount);

        writer.WriteInt32(lastest.startInputIndex);
        writer.WriteInt32(lastest.packetIndex);
        writer.WriteInt32(lastest.inputLenght);
        writer.WriteInt32(inputsToInclude);
        writer.WriteInt32(lastest.nextInputPacketIndex);
        writer.WriteInt32(savedStateIndex);
        
        for(int i = inputsToInclude - 1; i >= 0; i--) {
            recordQueue.ReserseGet(i).WriteInputsToStream(writer);
        }
    }

    public void AddNewSnapshot (InputSnapshot inputs) {
        //inputs.index = inputIndex;
        //inputs.packetIndex = packets[packets.Count - 1].packetIndex;

        // An input has been discarded! If the index of the discarded input is within the oldest packet, remove it
        int oldestIndex = 0;//recordQueue.OldestItem.index;
        if(recordQueue.Enqueue(inputs)) {
            if(packets[0].startInputIndex <= oldestIndex) {
                packets.RemoveAt(0);
                minPacketIndexInList++;
            }
        }

        InputPacket currentPacket = packets[packets.Count - 1];
        currentPacket.inputLenght++;
        packets[packets.Count - 1] = currentPacket;



        inputIndex++;
    }

    public void ClosePacket (int savedStateIndex) {
        packets.Add(new InputPacket(packetIndex, inputIndex, 0, packetIndex + 1, savedStateIndex));
        packetIndex++;
    }

    public InputSnapshot GetLastestSnapshot {
        get {
            return recordQueue.ReserseGet(0);
        }
    }

    public bool TryGetPacketByIndex (int packetIndex, out InputPacket packet) {
        if(packetIndex - minPacketIndexInList < packets.Count) {
            packet = packets[packetIndex - minPacketIndexInList];
            return true;
        } else {
            packet = default;
            return false;
        }
    }

    public bool TryGetInputsFromPacketIndex (int packetIndex, out int inputCount, out int queueIndex) {
        bool packetPresent = false;
        int packetListIndex = -1;

        for(int i = 0; i < packets.Count; i++) {
            if(packets[i].packetIndex == packetIndex) {
                packetListIndex = i;
                packetPresent = true;
                break;
            }
        }

        if(!packetPresent) {
            inputCount = 0;
            queueIndex = 0;
            return false;
        }

        int count = 0;
        for(int i = packetListIndex; i < packets.Count; i++) {
            count += packets[i].inputLenght;
        }

        inputCount = count;
        queueIndex = GetQueueIndex(packets[packetListIndex].startInputIndex);

        if(queueIndex == -1) {
            return false;
        }
        return true;
    }

    public int GetQueueIndex (int inputIndex) {
        int final = -1;
        recordQueue.ForeachElement((i, n) => {
            if(/*n.index*/0 == inputIndex) {
                final = i;
            }
        });
        return final;
    }

    public ref InputSnapshot GetInputByQueueIndexIndex (int queueIndex, int index) {
        return ref recordQueue.GetFromQueueIndexIndex(queueIndex, index);
    }
}

public class InputSnashotQueue {
    
    private List<InputPacket> packets;
    private Action<int, int> endOfPacketReached;
    private LimitedQueue<InputSnapshot> snapshotQueue;
    private int inputIndex = 0;
    private int lastInputReadIndex = 0;
    private byte previousRawButtons;

    public InputSnashotQueue (int maxInputs, Action<int, int> endOfPacketReached) {
        packets = new List<InputPacket>(8);
        snapshotQueue = new LimitedQueue<InputSnapshot>(maxInputs);
        this.endOfPacketReached = endOfPacketReached;
    }

    public void ReadInputsFromStream (NetworkReader reader) {

        int startIndex = reader.ReadInt32();
        int packetIndex = reader.ReadInt32();
        int packetCount = reader.ReadInt32();
        int count = reader.ReadInt32();
        int nextPacketReplay = reader.ReadInt32();
        int stateIndexReplay = reader.ReadInt32();

        if(packetCount > snapshotQueue.Capacity) {
            Debug.LogError("To much inputs to recieved in the inputs queue. Possible attack attempt?");
            return;
        }


        // -- Our goal here is to bring the inputIndex to startIndex + packetCount
        // /!\ If the inputs index arent lining up (inputIndex + 1 != startIndex) that means we've got to take of packet loss
        // > The inputs lost need to be wrapped up with a callback-less input packet
        // > The inputs lost we don't have must be duplicate of previous inputs (or plain empty in the worst case)
        // > The inputs lost we do have must be read from the stream and applied
        // /!\ If the inputs index ARE lining up, trash the useless inputs from the streams so they don't get in the way
        // -- After the lost inputs section/trashing has been done, take care of the new inputs as usual

        // Check if the inputs are lining up to detect potential input loss
        if(inputIndex != startIndex) {
            Debug.Log("Potential input loss");

            // Input loss has occured
            // We can start by wrapping up inputs with packets

            packets.Add(new InputPacket(packetIndex - 1, inputIndex, startIndex - inputIndex, 0, -1));
            packets.Add(new InputPacket(packetIndex, startIndex, packetCount, nextPacketReplay, stateIndexReplay));

            // Loop through inputIndex to startIndex + packetCount
            // but what if there's inputs included that are not needed??? HELP
            int loopStart = inputIndex;
            int loopEnd = (startIndex + packetCount);
            int offset = startIndex - inputIndex;
            int streamOffset = (loopEnd - count);

            //Debug.Log($"loopS {loopStart}, loopE {loopEnd}, startOff {startOffset}, streamOff {streamOffset}");
            int falseReads = loopStart - streamOffset;
            if(falseReads > 0) {
                for(int i = 0; i < falseReads; i++) {
                    InputSnapshot.ReadInputsFromStream(reader);
                }
            }

            for(int i = loopStart; i < loopEnd; i++) {
                int streamIndex = i - streamOffset;
                
                if(i <= lastInputReadIndex) {
                    continue;
                }

                InputSnapshot newInput = new InputSnapshot();
                if(streamIndex >= 0 && streamIndex < count) {
                    newInput = InputSnapshot.ReadInputsFromStream(reader);
                }

                // If the index of the end of the input buffer is bigger than the index of the input stream,
                // what the fuck should I do
                if(i < lastInputReadIndex) {
                    continue;
                }

                if(streamIndex < 0 || streamIndex >= count) {
                    Debug.LogError($"There's no inputs in the stream to use?!? {streamIndex}");
                    if(snapshotQueue.Count > 1 && streamIndex < 0) {

                        // Duplicating old inputs
                        newInput = snapshotQueue.ReserseGet(0);
                    }
                }

                //newInput.index = i;
                snapshotQueue.Enqueue(newInput);
            }
            inputIndex = loopEnd;

        } else {

            // Input loss hasn't occured
            // Read all inputs, trash the old, include the new
            
            packets.Add(new InputPacket(packetIndex, startIndex, math.min(packetCount, count), nextPacketReplay, stateIndexReplay));
            int startOffset = math.min(packetCount, count) - count;
            for(int i = 0; i < count; i++) {
                InputSnapshot newInput = InputSnapshot.ReadInputsFromStream(reader);

                // Imagine we're including a packet of 5 inputs at the index 4, but we're looping through 7 inputs to get it.
                // In that case, the first packet's index relative to the startIndex (4) would be 7-5. The first packet would
                // be -2, then -1 and only THEN we would get our first new packet at 0.
                if(i + startOffset < 0) continue;

                // Tag the packet with the correct index
                //newInput.index = startIndex + i + startOffset;
                inputIndex = startIndex + i + startOffset + 1;
                snapshotQueue.Enqueue(newInput);
            }
        }
    }

    public bool GetNextSnapshot (out InputSnapshot inputs, out byte previousRawButtons, out Action endOfPacketHandle) {
        if(!snapshotQueue.TryDequeue(out inputs)) {
            endOfPacketHandle = null;
            previousRawButtons = 0;
            return false;
        }
        previousRawButtons = this.previousRawButtons;
        this.previousRawButtons = inputs.GetButtonRaw();
        //lastInputReadIndex = inputs.index;

        // If the end of an input packed has been reached, send back calculated position to client including packet timestamp.
        // Do not do this with packet with stateIndex of -1 (lost packet wrappers)
        endOfPacketHandle = null;
        while(packets.Count > 1 && packets[0].startInputIndex + packets[0].inputLenght - 1 < /*inputs.index*/0) {
            packets.RemoveAt(0);
        }

        if(packets.Count == 0) {
            return true;
        }

        if(packets[0].startInputIndex + packets[0].inputLenght - 1 == /*inputs.index*/0) {
            if(packets[0].savedStateIndex != -1) {
                endOfPacketHandle = new Action(() => {
                    endOfPacketReached?.Invoke(packets[0].nextInputPacketIndex, packets[0].savedStateIndex);

                    packets.RemoveAt(0);
                });
            } else {
                packets.RemoveAt(0);
                endOfPacketHandle = null;
            }
        }
        
        return true;
    }

    public bool TryDitchUntilCount (int count) {
        if(snapshotQueue.Count <= count) {
            return false;
        } else {
            while(snapshotQueue.Count > count) {
                if(!snapshotQueue.TryDequeue(out InputSnapshot s)) {
                    break;
                }
            }
            return true;
        }
    }

    public int GetInputCount {
        get {
            return snapshotQueue.Count;
        }
    }

    public InputPacket GetLastestPacket {
        get {
            return packets[packets.Count - 1];
        }
    }

    public InputSnapshot GetLastestSnapshot {
        get {
            return snapshotQueue.ReserseGet(0);
        }
    }

    public int GetPacketDifference () {
        if(packets.Count < 2) {
            return -1;
        }
        return packets[1].packetIndex - packets[0].packetIndex;
    }
}
