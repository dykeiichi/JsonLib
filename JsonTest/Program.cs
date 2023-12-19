using System.Text;
using JSON.Entities;

namespace MainNamespace {
    public class MainClass {

        public const string StringData = @"{""Color"":""Rojo"",""Doors"":0,""Branch"":""Acura"",""HPs"":350,""Tires"":{""Width"":220,""AspectRatio"":55,""Architecture"":""R"",""Diameter"":17,""LoadIndex"":125,""SpeedRating"":""Z""},""Headlights"":{""Type"":""Led"",""Watts"":75,""Voltage"":12}}";
        public static readonly MemoryStream memoryStreamData;

        static MainClass() {
            memoryStreamData = new MemoryStream();
            StreamWriter writer = new (memoryStreamData);
            writer.Write(StringData);
            writer.Flush();
            memoryStreamData.Position = 0;
        }

        static int Main(string[] _) {
            JSONObject Jobject = new (memoryStreamData.ToArray());
            JSONObject JobjectTires = new (Jobject.GetObject("Tires"));
            JSONObject JobjectHeadlights = new (Jobject.GetObject("Headlights"));
            Automobil automovil = new Automobil(
                Jobject.GetString("Color") ?? "",
                (uint)(Jobject.GetUnsignedInteger("Doors") ?? 0U),
                Jobject.GetString("Branch") ?? "",
                (uint)(Jobject.GetUnsignedInteger("HPs") ?? 0U),
                new Tire(
                    (uint)(JobjectTires.GetUnsignedInteger("Width") ?? 0U),
                    (uint)(JobjectTires.GetUnsignedInteger("AspectRatio") ?? 0U),
                    (JobjectTires.GetString("Architecture") ?? string.Empty)[0],
                    (uint)(JobjectTires.GetUnsignedInteger("Diameter") ?? 0U),
                    (uint)(JobjectTires.GetUnsignedInteger("LoadIndex") ?? 0U),
                    (JobjectTires.GetString("SpeedRating") ?? string.Empty)[0]
                ),
                new Headligth(
                    JobjectHeadlights.GetString("Type") ?? "",
                    (uint)(JobjectHeadlights.GetUnsignedInteger("Watts") ?? 0U),
                    (uint)(JobjectHeadlights.GetUnsignedInteger("Voltage") ?? 0U)
                )
            );
            return 0;
        }
    }
}