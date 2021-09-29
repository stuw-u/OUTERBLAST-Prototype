using System;
using Unity.Mathematics;

namespace Blast.Collections {
    // New items that arriving when the count is full will discard old items. (This could cause serious issues for extremly laggy client)
    public class LimitedQueue<T> {
        T[] nodes;
        int current;
        int emptySpot;
        int _count;

        public int Count { get { return _count; } }
        public int Capacity { get { return nodes.Length; } }
        public T OldestItem { get { return nodes[current]; } }


        public T ReserseGet (int reverseIndex) {
            return nodes[intMod(emptySpot - 1 - reverseIndex, nodes.Length)];
        }

        public ref T GetFromQueueIndexIndex (int queueIndex, int index) {
            return ref nodes[intMod(queueIndex + index, nodes.Length)];
        }

        public void ForeachElement (Action<int, T> action) {
            for(int i = 0; i < nodes.Length; i++) {
                action(i, nodes[i]);
            }
        }

        private int intMod (int i, int m) {
            return i - (int)math.floor(i / (float)m) * m;
        }

        public LimitedQueue (int size) {
            nodes = new T[size];
            this.current = 0;
            this.emptySpot = 0;
            this._count = 0;
        }

        public bool Enqueue (T value) {
            bool hasPushedBack = false;

            // Push back old item to free place for new
            if(_count == nodes.Length) {
                _count--;
                hasPushedBack = true;

                current++;
                if(current >= nodes.Length) {
                    current = 0;
                }
            }

            nodes[emptySpot] = value;
            _count++;
            emptySpot++;
            if(emptySpot >= nodes.Length) {
                emptySpot = 0;
            }

            return hasPushedBack;
        }

        public bool TryDequeue (out T item) {
            if(_count - 1 < 0) {
                item = default;
                return false;
            }

            item = nodes[current];
            _count--;
            current++;
            if(current >= nodes.Length) {
                current = 0;
            }
            return true;
        }
    }
}