using System;

namespace API.Extensions
{
    public static class DateTimeExtension
    {
       public static int  CalculateAge(this DateTime dbo)
       {
           var today=DateTime.Today;
           var age=today.Year-dbo.Year;
           if(dbo.Date>today.AddYears(-age))
           {
               --age;
           }
           return age;
       }
    }
}