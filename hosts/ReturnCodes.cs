using System;

namespace hosts
{
    public enum ReturnCodes
    {
        None = Int32.MinValue,
        Success = 0,
        BadServerType = -1,
        ServerTargetNull = -2
    }
}
