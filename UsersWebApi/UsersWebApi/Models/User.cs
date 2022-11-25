using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UsersWebApi.Models
{
    public class User 
    {
        public int? Id { get; set; }

        public string Password { get; set; }

        public string Name { get; set; }

        public string  Email { get; set; }

        public string Phone { get; set; }

        public int Role { get; set; }

        public string? VerificationToken { get; set; }
    }
}
