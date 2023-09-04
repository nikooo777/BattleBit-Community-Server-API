namespace SAT.Utils;

public class Binary
{
    public static void DecodeTools(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var ser = new BinaryReader(ms))
        {
            var size = (int)ser.ReadUInt32();
            size = data.Length - 4;
            Console.WriteLine($"size: {size} bytes - computed: {data.Length} bytes");
            while (size > 0)
            {
                size -= 10; // 10 bytes: 2 for id, 4 for value01, and 4 for value02

                var id = ser.ReadUInt16();
                var value01 = ser.ReadUInt32();
                var value02 = ser.ReadUInt32();

                Console.WriteLine($"tool_id: {id} kills: {value01} max_distance: {value02}");
            }
        }
    }
}