
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MagPack.Messages
{

    public class Employee
    {

        public int EmployeeID
        {
            get;
            set;
        }

        public string LastName
        {
            get;
            set;
        }

        public string FirstName
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string TitleOfCourtesy { get; set; }

        public DateTime BirthDate { get; set; }

        public DateTime HireDate { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string Region { get; set; }

        public string PostalCode { get; set; }

        public string Country { get; set; }

        public string HomePhone { get; set; }

        public string Extension { get; set; }

        public string Photo { get; set; }

        public string Notes { get; set; }
    }

    public class SearchEmployee
    {

        public SearchEmployee()
        {
            Quantity = 1;
        }

        public int Quantity
        {
            get; set;
        }
    }

    public class SearchCustomer
    {
        public SearchCustomer()
        {
            Quantity = 1;
        }

        public int Quantity
        {
            get; set;
        }
    }

    public class Customer
    {

        public string CustomerID { get; set; }

        public string CompanyName { get; set; }

        public string ContactName { get; set; }

        public string ContactTitle { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string PostalCode { get; set; }

        public string Country { get; set; }

        public string Phone { get; set; }

        public string Fax { get; set; }
    }

}
