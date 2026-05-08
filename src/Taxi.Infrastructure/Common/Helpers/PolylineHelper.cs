using System.Text;

namespace Taxi.Infrastructure.Common.Helpers;

public static class PolylineHelper
{
    public static string Encode(IEnumerable<(double Lat, double Lng)> points)
    {
        var str = new StringBuilder();
        int lastLat = 0;
        int lastLng = 0;

        foreach (var point in points)
        {
            int lat = (int)Math.Round(point.Lat * 1e5);
            int lng = (int)Math.Round(point.Lng * 1e5);

            EncodeValue(lat - lastLat, str);
            EncodeValue(lng - lastLng, str);

            lastLat = lat;
            lastLng = lng;
        }

        return str.ToString();
    }

    public static List<(double Lat, double Lng)> Decode(string encodedPolyline)
    {
        if (string.IsNullOrEmpty(encodedPolyline))
        {
            return [];
        }

        var polylineChars = encodedPolyline.ToCharArray();
        int index = 0;

        var points = new List<(double Lat, double Lng)>();
        int lat = 0;
        int lng = 0;

        while (index < polylineChars.Length)
        {
            // Decode Latitude
            int sum = 0;
            int shifter = 0;
            int next5Bits;
            do
            {
                next5Bits = polylineChars[index++] - 63;
                sum |= (next5Bits & 31) << shifter;
                shifter += 5;
            }
            while (next5Bits >= 32 && index < polylineChars.Length);

            if (index >= polylineChars.Length && next5Bits >= 32)
            {
                break;
            }

            lat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

            // Decode Longitude
            sum = 0;
            shifter = 0;
            do
            {
                next5Bits = polylineChars[index++] - 63;
                sum |= (next5Bits & 31) << shifter;
                shifter += 5;
            }
            while (next5Bits >= 32 && index < polylineChars.Length);

            if (index >= polylineChars.Length && next5Bits >= 32 && points.Count > 0)
            {
                break;
            }

            lng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

            points.Add((lat / 1e5, lng / 1e5));
        }

        return points;
    }

    private static void EncodeValue(int value, StringBuilder str)
    {
        value = value < 0 ? ~(value << 1) : (value << 1);

        while (value >= 0x20)
        {
            str.Append((char)((0x20 | (value & 0x1f)) + 63));
            value >>= 5;
        }

        str.Append((char)(value + 63));
    }
}
