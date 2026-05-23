using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Models
{
    public class ContactModel
    {
        public string DisplayName { get; set; }
        public List<string> Phones { get; set; } = new List<string>();
        public List<string> Emails { get; set; } = new List<string>();
    }
}
