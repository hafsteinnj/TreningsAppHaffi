using System;

namespace TreningsAppHaffi.Data
{
    public class TestEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int JobId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Minutes { get; set; }
        public bool Hidden { get; set; }
    }
}
