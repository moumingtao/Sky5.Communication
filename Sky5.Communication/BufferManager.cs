using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sky5.Communication
{
    class BufferManager
    {
        public int BlockMinSize;
        readonly BlockLink FirstLink = new BlockLink();
        class BlockLink
        {
            public WeakReference<byte[]> Bytes;
            public int FreeMaxSegement;
            public int Cursor;
            byte[] bytes;// 保证空闲的片段和空闲的片段不接触，如果接触就合并，非空闲的片段同理
            public bool BeginVisit() => Bytes.TryGetTarget(out bytes);
            public void EndVisit() => bytes = null;
            public bool TryGetBuffer(int size, out ArraySegment<byte> memory)
            {
                SetFlag(Cursor, false, size);
                SetFlag(Cursor + FlagSize + size, false, size);
                Cursor += size + FlagSize * 2;
                GetIsFree(Cursor);
            }
            public void FreeBuffer(ArraySegment<byte> seg)
            {
                // 它的上一个片段和下一个片段一定是空闲的，且合并后的片段是空闲的
                int mergedPosition = seg.Offset > FlagSize// 根据指定片段的开始位置判断合并后片段的开始位置
                    ? seg.Offset - FlagSize * 3 - GetSize(seg.Offset - FlagSize * 2)// 前面有片段，合并后片段的开始位置是上一个片段的开始位置
                    : seg.Offset - FlagSize;// 前面没有片段，合并后片段的开始位置就是当前片段开始位置
                int mergedEnd = seg.Offset + seg.Count + FlagSize * 3 > bytes.Length// 根据指定片段后面是否还有片段判断合并后片段的结束位置
                    ? bytes.Length// 该片段后的字节数不足以构成一个片段
                    : seg.Offset + seg.Count + FlagSize
            }
            const int FlagSize = 3;
            bool GetIsFree(int position) => (bytes[position] & 0b10000000) != 0;
            int GetSize(int position) => ((bytes[position] & 0b01111111) << 16) | (bytes[position + 1] << 8) | (bytes[position + 2]);
            void SetFlag(int position, bool isFree, int size)
            {
                bytes[position] = (byte)(size >> 16);
                if (isFree) bytes[position] |= 0b10000000;
                bytes[position + 1] = (byte)(size >> 8);
                bytes[position + 2] = (byte)size;

            }
        }
        public ArraySegment<byte> GetBuffer(int size) => FirstLink.GetBuffer(size, this);
        public void FreeBuffer(ArraySegment<byte> memory)
        {
        }
    }
}
