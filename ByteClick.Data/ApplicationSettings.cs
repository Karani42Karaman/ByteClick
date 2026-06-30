using System.ComponentModel.DataAnnotations;


namespace ByteClick.Data
{
    public class ApplicationSettings
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public bool SettingValue { get; set; }

        [Required]
        public string ApplicationMethodName { get; set; }

        public DateTime DateTime { get; set; }
    }
}
