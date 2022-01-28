using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ConcurrencyProblem
{
    [Table("mytable")]
    public class MyEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string ColA { get; set; }
        [Required]
        public string ColB { get; set; }

        public int? MySeq { get; set; }

    }
}