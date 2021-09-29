using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System;
using System.Collections.Generic;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using Blast.Collections;

/// <summary>
/// Controller data at a specific frame
/// </summary>
[Serializable]
public struct InputSnapshot {

    #region Data
    [SerializeField] private byte buttons;
    [SerializeField] private byte _moveAxisX;
    [SerializeField] private byte _moveAxisY;
    [SerializeField] private float2 _lookAxis;
    public byte selectedFixedInventoryItem;
    public uint selectedInventoryItemUID;

    public void SetButton (bool value, byte index) {
        buttons &= (byte)(~(1 << index));
        buttons |= (byte)(math.select(0, 1, value) << index);
    }
    public void SetMoveAxisRaw (byte moveAxisX, byte moveAxisY) {
        _moveAxisX = moveAxisX;
        _moveAxisY = moveAxisY;
    }
    public void GetMoveAxisRaw (out byte moveAxisX, out byte moveAxisY) {
        moveAxisX = _moveAxisX;
        moveAxisY = _moveAxisY;
    }
    public byte GetButtonRaw () {
        return buttons;
    }

    internal bool GetButton (byte itemMapping, byte lastButtonRaw) {
        throw new NotImplementedException();
    }

    public void SetButtonRaw (byte buttons) {
        this.buttons = buttons;
    }
    public bool GetButton (byte index) {
        return ((buttons >> index) & 1) == 1;
    }
    public bool GetButtonDown (byte index, byte previousRaw) {
        return (((buttons & ~previousRaw) >> index) & 1) == 1;
    }
    public bool GetButtonUp (byte index, byte previousRaw) {
        return (((~buttons & previousRaw) >> index) & 1) == 1;
    }
    public float2 moveAxis {
        set {
            float2 n = value;
            float len = math.length(n);
            if(len > 1f) {
                n /= len;
            }
            _moveAxisX = (byte)math.select(127, math.floor((n.x + 1f) * 127f), value.x != 0f);
            _moveAxisY = (byte)math.select(127, math.floor((n.y + 1f) * 127f), value.y != 0f);
        }
        get {
            return new float2(_moveAxisX / 127f - 1f, _moveAxisY / 127f - 1f);
        }
    }
    public float2 lookAxis {
        set {
            _lookAxis = value;
        }
        get {
            return _lookAxis;
        }
    }
    public bool isMoving {
        get {
            return _moveAxisX != 127 || _moveAxisY != 127;
        }
    }
    public float3 Get3DMoveAxis () {
        float2 moveAxis2D = moveAxis;
        return new float3(moveAxis2D.x, 0f, moveAxis2D.y);
    }
    #endregion

    public static InputSnapshot ReadInputsFromStream (NetworkReader reader) {
        InputSnapshot inputs = new InputSnapshot();

        inputs.buttons = (byte)reader.ReadByte();
        inputs._moveAxisX = (byte)reader.ReadByte();
        inputs._moveAxisY = (byte)reader.ReadByte();
        inputs._lookAxis = new float2(reader.ReadSingle(), reader.ReadSingle());
        inputs.selectedFixedInventoryItem = reader.ReadByteBits(4);

        return inputs;
    }

    public void WriteInputsToStream (NetworkWriter writer) {
        writer.WriteByte(buttons);
        writer.WriteByte(_moveAxisX);
        writer.WriteByte(_moveAxisY);
        writer.WriteSingle(_lookAxis.x);
        writer.WriteSingle(_lookAxis.y);
        writer.WriteBits(selectedFixedInventoryItem, 4);
    }
}
