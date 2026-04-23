using System.ComponentModel;

namespace LCB.Domain.Enums;

public enum StatusResultEnum
{
    [Description("processed")]
    Processed,

    [Description("ignored_duplicate")]
    Duplicate,

    [Description("error")]
    Error
}