using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicSugar;

namespace RazorHelperUnitTests {

    /// <summary>
    /// A test class
    /// </summary>
    /// 
    public class Person {

        public static List<Person> GetPeopleList() {

            var people = DS.List(
                new Person() { LastName = "Descartes",   FirstName = "Rene",   Age = 20 },
                new Person() { LastName = "Montesquieu", FirstName = "Gerard", Age = 40 },
                new Person() { LastName = "Rousseau",    FirstName = "JJ",     Age = 60 }
            );
            return people;
        }

        public string LastName;
        public string FirstName  { get; set; }
        public int Age           { get; set; }
        public DateTime BirthDay { get; set; }       
    }
}
