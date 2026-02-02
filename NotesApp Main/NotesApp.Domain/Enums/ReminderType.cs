using System;

namespace NotesApp.Domain.Enums
{
    [Flags]
    public enum ReminderType
    {
        InApp = 1,
        Email = 2,
        Push = 4
    }
}
