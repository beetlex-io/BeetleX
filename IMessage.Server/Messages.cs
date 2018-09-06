using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeetleX.Buffers;
using BeetleX.Packets;

namespace Messages
{

    public class SearchEmployee : BeetleX.Packets.IMessage
    {
        public SearchEmployee()
        {
            Quantity = 1;
        }

        public int Quantity
        {
            get; set;
        }

        public void Load(IBinaryReader reader)
        {
            Quantity = reader.ReadInt32();
        }

        public void Save(IBinaryWriter writer)
        {
            writer.Write(Quantity);
        }
    }


    public class Employee : BeetleX.Packets.IMessage
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

        public void Load(IBinaryReader reader)
        {
            EmployeeID = reader.ReadInt32();
            LastName = reader.ReadShortUTF();
            FirstName = reader.ReadShortUTF();
            Title = reader.ReadShortUTF();
            TitleOfCourtesy = reader.ReadShortUTF();
            BirthDate = reader.ReadDateTime();
            HireDate = reader.ReadDateTime();
            Address = reader.ReadShortUTF();
            City = reader.ReadShortUTF();
            Region = reader.ReadShortUTF();
            PostalCode = reader.ReadShortUTF();
            Country = reader.ReadShortUTF();
            HomePhone = reader.ReadShortUTF();
            Extension = reader.ReadShortUTF();
            Photo = reader.ReadShortUTF();
            Notes = reader.ReadShortUTF();
        }

        public void Save(IBinaryWriter writer)
        {
            writer.Write(EmployeeID);
            writer.WriteShortUTF(LastName);
            writer.WriteShortUTF(FirstName);
            writer.WriteShortUTF(Title);
            writer.WriteShortUTF(TitleOfCourtesy);
            writer.Write(BirthDate);
            writer.Write(HireDate);
            writer.WriteShortUTF(Address);
            writer.WriteShortUTF(City);
            writer.WriteShortUTF(Region);
            writer.WriteShortUTF(PostalCode);
            writer.WriteShortUTF(Country);
            writer.WriteShortUTF(HomePhone);
            writer.WriteShortUTF(Extension);
            writer.WriteShortUTF(Photo);
            writer.WriteShortUTF(Notes);

        }

        public static Employee GetEmployee()
        {
            Employee result = new Employee();
            result.EmployeeID = 1;
            result.LastName = "Davolio";
            result.FirstName = "Nancy";
            result.Title = "Sales Representative";
            result.TitleOfCourtesy = "MS.";
            result.BirthDate = DateTime.Parse("1968-12-08");
            result.HireDate = DateTime.Parse("1992-05-01");
            result.Address = "507 - 20th Ave. E.Apt. 2A";
            result.City = "Seattle";
            result.Region = "WA";
            result.PostalCode = "98122";
            result.Country = "USA";
            result.HomePhone = "(206) 555-9857";
            result.Extension = "5467";
            result.Photo = "EmpID1.bmp";
            result.Notes = "Education includes a BA in psychology from Colorado State University.  She also completed &quot;The Art of the Cold Call.&quot;  Nancy is a member of Toastmasters International.";
            return result;
        }
    }
}
