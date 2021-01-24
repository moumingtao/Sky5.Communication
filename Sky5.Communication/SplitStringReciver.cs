using System;
using System.Net;
using System.Text;

namespace Sky5.Communication
{
    public class SplitStringReciver : StringReciver
    {
        StringBuilder sb = new StringBuilder();
        public string Spliter = "\r\n";
        volatile int findIndex;
#if StringNotToReadOnlySpan
        protected override bool ContinueRecv(EndPoint remote, char[] chars, int offset, int count)
        {
            sb.Append(chars, offset, count);
#else
        protected override bool ContinueRecv(EndPoint remote, ReadOnlySpan<char> content)
        {
            sb.Append(content);
#endif
            var end = sb.Length - Spliter.Length;
            while (findIndex <= end)
            {
                if (Find())
                {
                    var line = sb.ToString(0, findIndex);
                    sb.Remove(0, line.Length + Spliter.Length);
                    findIndex = 0;
                    if (!ContinueLine(remote, line))
                        return false;
                    end = sb.Length - Spliter.Length;
                }
                else
                    findIndex++;
            }
            return true;
        }
        bool Find()
        {
            for (int i = 0; i < Spliter.Length; i++)
            {
                if (sb[findIndex + i] != Spliter[i])
                    return false;
            }
            return true;
        }
        protected virtual bool ContinueLine(EndPoint remote, string content) => true;
    }
}
