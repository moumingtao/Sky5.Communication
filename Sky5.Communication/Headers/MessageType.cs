using System;
using System.Collections.Generic;
using System.Text;

namespace Sky5.Communication.Headers
{
    class MessageType
    {
        Dictionary<ushort, Type> CodeToType = new Dictionary<ushort, Type>();
        Dictionary<Type, ushort> TypeToCode = new Dictionary<Type, ushort>();
        public void RegisterType(ushort code, Type type)
        {
            CodeToType.Add(code, type);
            TypeToCode.Add(type, code);
        }
        public Type GetType(ushort code) => CodeToType[code];
        public int GetCode(Type type) => TypeToCode[type];

    }
}
