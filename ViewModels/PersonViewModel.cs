﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModels
{
    public class PersonViewModel
    {
        public PersonViewModel()
        {
            this.Following = new List<PersonViewModel>();
            this.Intersts = new List<ThingViewModel>();
        }

        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
        public IList<PersonViewModel> Following { get; set; }
        public IList<ThingViewModel> Intersts { get; set; }

        public string Location { get; set; }

        public string FullName { get { return string.Format("{0} {1}", FirstName, LastName); } }
        public string MailToEmail { get { return string.Format("mailto:{0}", Email);}}
    }
}
