
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var module = new Module11004()
{
    X = 6.0f,
    Y = 2.0f,
    Angle = 1.57f,
    Confidence = 0.9f
};
//Console.WriteLine(BitConverter.ToString(MakePackage(1,11004,module)));
TcpClient client = new TcpClient();
client.SendTimeout = 120000;
client.ReceiveTimeout = 120000;
await client.ConnectAsync("127.0.0.1", 9999);
var stream = client.GetStream();
var res1 = await Module11004TaskAsync(stream, 1, module);
Console.WriteLine(JsonSerializer.Serialize(res1));


//5A 01 00 01 00 00 00 3C 2A FC 00 00 00 00 00 00 
//7B 22 72 65 74 5F 63 6F 64 65 22 3A 30 2C 22 78 22 3A 36 2E 30 2C 22 79 22 3A 32 2E 30 2C 22 61 6E 67 6C 65 22 3A 31 2E 35 37 2C 22 63 6F 6E 66 69 64 65 6E 63 65 22 3A 30 2E 39 7D
//5A-01-00-01-00-00-00-38-2A-FC-00-00-00-00-00-00
//7B-22-72-65-74-5F-63-6F-64-65-22-3A-30-2C-22-78-22-3A-36-2C-22-79-22-3A-32-2C-22-61-6E-67-6C-65-22-3A-31-2E-35-37-2C-22-63-6F-6E-66-69-64-65-6E-63-65-22-3A-30-2E-39-7D

static async Task<ModuleResponse11004> Module11004TaskAsync(Stream networkStream,ushort reqId, Module11004 module)
{
    var sendBytes = MakePackage(reqId, 11004, module);
    await networkStream.WriteAsync(sendBytes);
    byte[] headBuffer = new byte[16];
    var readCount =await networkStream.ReadAsync(headBuffer, 0, 16);
    if (readCount!=16)
    {
        throw new Exception($"response head length:{readCount}");
    }

    Memory<byte> head = headBuffer;
    var length = (int)BitConverter.ToUInt32(head.Slice(4,4).ToArray().Reverse().ToArray());
    if (length > 0)
    {
        byte[] jsonBytes = new byte[length];
        byte[] temp = new byte[1024];
        while (length>0)
        {
            readCount = await networkStream.ReadAsync(temp, 0, 1024);
            if (readCount > 0)
            {
                Array.Copy(temp,0,jsonBytes,jsonBytes.Length-length,readCount);
                length -= readCount;   
            }
        }

        var jsonString = Encoding.ASCII.GetString(jsonBytes);
        Console.WriteLine(jsonString);
        return JsonSerializer.Deserialize<ModuleResponse11004>(jsonString);
    }
    else
    {
        throw new Exception("response json length equal zero");
    }
}

static byte[] MakePackage(ushort reqId, ushort moduleId, object jsonObject)
{
    byte[]? jsonBytes = null;
    if (jsonObject != null)
    {
        var json = JsonSerializer.Serialize(jsonObject);
        Console.WriteLine(json);
        jsonBytes = Encoding.ASCII.GetBytes(json);
    }
    using var ms = new MemoryStream();
    ms.WriteByte(0x5A);
    ms.WriteByte(0x01);
    ms.Write(BigEndianess(reqId));
    ms.Write(BigEndianess(jsonBytes?.Length??0));
    ms.Write(BigEndianess(moduleId));
    ms.WriteByte(0x00);
    ms.WriteByte(0x00);
    ms.WriteByte(0x00);
    ms.WriteByte(0x00);
    ms.WriteByte(0x00);
    ms.WriteByte(0x00);
    if (jsonBytes != null)
    {
        ms.Write(jsonBytes);
    }

    return ms.ToArray();
}

static byte[] BigEndianess<T>(T source) where T:struct
{
    byte[]? raw = null;
    if(source is ushort ushortValue)
        raw = BitConverter.GetBytes(ushortValue);
    else if(source is int intValue)
        raw = BitConverter.GetBytes(intValue);
    else if(source is uint uintValue)
        raw = BitConverter.GetBytes(uintValue);
    else if (source is ulong ulongValue)
        raw = BitConverter.GetBytes(ulongValue);
    else
        throw new NotSupportedException(source.GetType().Name);
    if (BitConverter.IsLittleEndian)
    {
        Array.Reverse(raw);
    }
    return raw;
}

class Module11004
{
    [JsonPropertyName("ret_code")]
    public int RetCode { get; set; }
    [JsonPropertyName("x")]
    public float X { get; set; }
    [JsonPropertyName("y")]
    public float Y { get; set; }
    [JsonPropertyName("angle")]
    public float Angle { get; set; }
    [JsonPropertyName("confidence")]
    public float Confidence { get; set; }
}
class ModuleResponse11004
{
    [JsonPropertyName("ret_code")]
    public int RetCode { get; set; }
    [JsonPropertyName("create_on")]
    public string? CreateOn { get; set; }
    [JsonPropertyName("err_msg")]
    public string? ErrMsg { get; set; }
}

