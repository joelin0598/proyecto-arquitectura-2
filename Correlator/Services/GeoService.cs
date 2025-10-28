using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Correlator.Services
{
    public class GeoService
    {
        public static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radio de la Tierra en km
            var lat1Rad = ToRadians(lat1);
            var lon1Rad = ToRadians(lon1);
            var lat2Rad = ToRadians(lat2);
            var lon2Rad = ToRadians(lon2);

            var dlat = lat2Rad - lat1Rad;
            var dlon = lon2Rad - lon1Rad;

            var a = Math.Sin(dlat / 2) * Math.Sin(dlat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(dlon / 2) * Math.Sin(dlon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;  // Distancia en km
        }

        public static double ToRadians(double degree)
        {
            return degree * (Math.PI / 180);
        }
    }
}
