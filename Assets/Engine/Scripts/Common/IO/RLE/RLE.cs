using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.Scripts.Common.IO.RLE
{
    public class RLE<T>: IBinarizable where T : IComparable<T>, IBinarizable, new ()
    {
        private int m_decompressedLength;

        public List<RLEDataPair<T>> List { get; private set; }

        public RLE()
        {
            m_decompressedLength = 0;
            List = new List<RLEDataPair<T>>();
        }

        public RLE(int capacity)
        {
            m_decompressedLength = 0;
            List = new List<RLEDataPair<T>>(capacity);
        }

        public RLE(RLEDataPair<T> [] arr)
        {
            Assign(arr);
        }

        public RLE(List<RLEDataPair<T>> arr)
        {
            Assign(arr);
        }

        public void Assign(RLEDataPair<T>[] arr)
        {
            m_decompressedLength = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                RLEDataPair<T> t = arr[i];
                m_decompressedLength += t.Key;
            }
            List = new List<RLEDataPair<T>>(arr);
        }

        public void Assign(List<RLEDataPair<T>> arr)
        {
            m_decompressedLength = 0;
            for (int i = 0; i < arr.Count; i++)
            {
                RLEDataPair<T> t = arr[i];
                m_decompressedLength += t.Key;
            }
            List = arr;
        }

        public void Reset()
        {
            m_decompressedLength = 0;
            List.Clear();
        }

        // Overloading this operator so that the class can be used as an array
        public T this[int index]
        {
            get { return List[GetPositionFromIndex(index)].Value; }
            set { SetDataOnIndex(index, value); }
        }

        public int GetPositionFromIndex(int index)
        {
            // Sum list keys until the sum reaches the desired size (index)
            int i, sum = 0;
            for (i = 0; i < List.Count; i++)
            {
                sum += List[i].Key;
                if (sum > index)
                    break;
            }

            // Return value in list. Throws an exception if trying to access a wrong index
            return i;
        }

        public int GetPositionFromIndex(int index, out int position)
        {
            // Sum list keys until the sum reaches the desired size (index)
            int i, sum = 0;
            for (i = 0; i < List.Count; i++)
            {
                sum += List[i].Key;
                if (sum > index)
                    break;
            }

            // Return value in list. Throws an exception if trying to access a wrong index
            position = (sum<<1) - index; // sum + (sum - index)
            return i;
        }

        public void SetDataOnIndex(int index, T value)
        {
            int sum = 0;
            for (int i = 0; i < List.Count; i++)
            {
                sum += List[i].Key;
                if (sum <= index)
                    continue;

                // Do nothing if we are trying to set the same value
                // This further simplifies logic behind merging adjacent runs
                // because we now know that the position on index is going to
                // contain different data
                if (List[i].Value.CompareTo(value)==0)
                    return;

                var maxPosWithinRun = List[i].Key - 1;
                var posWithinRun = index - (sum - List[i].Key);

                // If we are at run's edge let's merge adjacent runs if possible
                if (List[i].Key == 1)
                {
                    var newIndex = MergeWithLeft(i, value);
                    MergeWithRight(newIndex, value);
                }
                else
                if (posWithinRun == 0)
                {
                    MergeWithLeft(i, value);
                }
                else
                if (posWithinRun == maxPosWithinRun)
                {
                    MergeWithRight(i, value);
                }
                else
                // Nothing to merge. Split the old run and place a new one in the middle
                {
                    var tmpCnt = List[i].Key;

                    List[i] = new RLEDataPair<T>(posWithinRun, List[i].Value);
                    List.Insert(i + 1, new RLEDataPair<T>(1, value));
                    List.Insert(i + 2, new RLEDataPair<T>(tmpCnt - List[i].Key - 1, List[i].Value));
                }

                return;
            }
        }

        private int MergeWithLeft(int index, T value)
		{
			// E.g. [1 1][2 2 2][1] into [1 1][9][2 2][1]
			if (index==0 || List[index - 1].Value.CompareTo(value) != 0)
			{
                List[index] = new RLEDataPair<T>(List[index].Key - 1, List[index].Value); // --List[index].Key;
				List.Insert(index, new RLEDataPair<T>(1, value)); // prepend a new value on a given index
				return index;
			}
			else
			// E.g. [1 1][2 2 2][1] into [1 1 1][2 2][1]
			{
                List[index] = new RLEDataPair<T>(List[index].Key - 1, List[index].Value); // --List[index].Key;
				if (List[index].Key == 0)
				{
					// [1 1][2][3 3] into [1 1 1][3 3]
					List.RemoveAt(index);
				}
				// This following can happen if run has a length of one
				// and merge with left and right needs to be done
				else if(List[index].Value.CompareTo(List[index - 1].Value) == 0)
				{					
					// [1 1][1][3 3] into [1 1 1][3 3]
                    List[index - 1] = new RLEDataPair<T>(List[index - 1].Key + List[index].Key + 1, List[index - 1].Value); // List[index - 1].Key += List[index].Key + 1;					
					List.RemoveAt(index);
					return index-1;
				}

                List[index] = new RLEDataPair<T>(List[index].Key + 1, List[index].Value); // ++List[index].Key;
				return index - 1;
			}
		}

        private int MergeWithRight(int index, T value)
		{
			// E.g. [1 1][2 2 2][1] into [1 1][2 2][9][1]
			if (index+1>=List.Count || List[index + 1].Value.CompareTo(value) != 0)
			{
                List[index] = new RLEDataPair<T>(List[index].Key - 1, List[index].Value); // --List[index].Key;
				List.Insert(index + 1, new RLEDataPair<T>(1, value));
				return index + 1;
			}
			else
			// E.g. [1 1][2 2 2][1] into [1 1][2 2][1 1]
			{
                List[index] = new RLEDataPair<T>(List[index].Key - 1, List[index].Value); // --List[index].Key;
				if (List[index].Key==0)
				{
					// [1 1][2][3 3] into [1 1][3 3 3]
					List.RemoveAt(index);
					--index;
				}
				// This following can happen if run has a length of one
				// and merge with left and right needs to be done
				else if (List[index].Value.CompareTo(List[index + 1].Value)==0)
				{					
					// [2 2][1][1 1] into [2 2][1 1 1]
                    List[index + 1] = new RLEDataPair<T>(List[index + 1].Key + List[index].Key + 1, List[index + 1].Value); // List[index + 1].Key += List[index].Key + 1;					
                    List.RemoveAt(index);
					return index;
				}

                List[index] = new RLEDataPair<T>(List[index].Key + 1, List[index].Value); // ++List[index].Key;
				return index + 1;
			}
		}

        public void Compress(ref T[] data)
        {
            if (data == null || data.Length == 0)
                return;

            m_decompressedLength += data.Length;

            T prevVal = data[0];
            int cnt = 1;

            for (int i = 1; i < data.Length; i++)
            {
                // Update counter while data does not change
                if (data[i].CompareTo(prevVal) == 0)
                    cnt++;
                else
                {
                    // Data changed. Add collected cnt/data pair to list
                    List.Add(new RLEDataPair<T>(cnt, prevVal));

                    prevVal = data[i];
                    cnt = 1;
                }
            }
            
            List.Add(new RLEDataPair<T>(cnt, prevVal));
        }

        public void Decompress(ref T[] outData)
        {
            int offset = 0;
            for (int i = 0; i < List.Count; i++)
            {
                RLEDataPair<T> pair = List[i];
                for (int j = 0; j < pair.Key; j++)
                    outData[offset + j] = pair.Value;

                offset += pair.Key;
            }
        }

        public T[] Decompress()
        {
            T[] data = new T[m_decompressedLength];

            // 2nd pass - decompress data
            int offset = 0;
            for (int i = 0; i < List.Count; i++)
            {
                RLEDataPair<T> pair = List[i];
                for (int j = 0; j < pair.Key; j++)
                    data[offset + j] = pair.Value;

                offset += pair.Key;
            }

            return data;
        }

        public void Binarize(BinaryWriter bw)
        {
            bw.Write(List.Count);
            for (int i = 0; i < List.Count; i++)
            {
                bw.Write(List[i].Key);
                List[i].Value.Binarize(bw);
            }
        }

        private readonly T m_dummy = new T();

        public void Debinarize(BinaryReader br)
        {
            List.Clear();
            m_decompressedLength = 0;

            int cnt = br.ReadInt32();
            for (int i = 0; i < cnt; i++)
            {
                // Read key
                int key = br.ReadInt32();                

                // Read value
                m_dummy.Debinarize(br);

                // Add new element to list
                List.Add(new RLEDataPair<T>(key, m_dummy));
                m_decompressedLength += key;
            }
        }
    }
}