using System.ComponentModel.DataAnnotations.Schema;

namespace SqlObjectCopy.Models
{
    public class Column
    {
        public int ObjectId { get; set; }
        public string Name { get; set; }
        public bool IsComputed { get; set; }
        public byte SystemTypeId { get; set; }
    }
}
