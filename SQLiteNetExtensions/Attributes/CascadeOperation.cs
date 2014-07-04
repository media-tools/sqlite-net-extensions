using System;

namespace SQLiteNetExtensions.Attributes
{
    [Flags]
    public enum CascadeOperation {
        None                        = 0,
        CascadeRead                 = 1 << 1,
        CascadeUpdate               = 1 << 2,
        CascadeDelete               = 1 << 3,
        All                         = CascadeRead | CascadeUpdate | CascadeDelete
    }
    
}
