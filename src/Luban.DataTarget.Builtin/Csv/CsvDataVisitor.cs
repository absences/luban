using Luban.Datas;
using Luban.DataVisitors;
using Luban.Utils;
using System.Text;
using System.Text.Json;

namespace Luban.DataExporter.Builtin.Csv
{
    public class CsvSet
    {
        public string headType { get; set; }
        public string dataType { get; set; }

        public static string GetHeadType(string set)
        {
            if (string.IsNullOrEmpty(set))
            {
                return string.Empty;
            }
            var s = JsonSerializer.Deserialize<CsvSet>(set);
            return s.headType;
        }
        public static string GetDataType(string set)
        {
            if (string.IsNullOrEmpty(set))
            {
                return string.Empty;
            }
            var s = JsonSerializer.Deserialize<CsvSet>(set);
            return s.dataType;
        }
    }


    internal class CsvDataVisitor : IDataActionVisitor<StringBuilder>
    {

        private static CsvJsonVisitor JsonIns { get; } = new();
        public static CsvDataVisitor Ins { get; } = new();

        public void Accept(DBool type, StringBuilder x)
        {
            x.Append(type.Value);
        }

        public void Accept(DByte type, StringBuilder x)
        {
            x.Append(type);
        }

        public void Accept(DShort type, StringBuilder x)
        {
            x.Append(type);
        }

        public void Accept(DInt type, StringBuilder x)
        {
            x.Append(type);
        }

        public void Accept(DLong type, StringBuilder x)
        {
            x.Append(type);
        }

        public void Accept(DFloat type, StringBuilder x)
        {
            x.Append(type);
        }

        public void Accept(DDouble type, StringBuilder x)
        {
            x.Append(type);
        }

        public void Accept(DEnum type, StringBuilder x)
        {
            var t = type.Type;

            var item = t.DefEnum.Items.First(f => f.IntValue == type.Value);

            x.Append(item.Name);
        }

        public void Accept(DString type, StringBuilder x)
        {
            x.Append(DataUtil.EscapeString(type.Value));
        }

        public void Accept(DDateTime type, StringBuilder x)
        {
            x.Append(type.UnixTimeOfCurrentContext());
        }

        public void Accept(DBean type, StringBuilder x)
        {
            var set = type.TType.DefBean.CsvSet;

            var headType = CsvSet.GetHeadType(set);
            var dataType = CsvSet.GetDataType(set);
            var count = 0;
            var fieldIdx = 0;

            for (int i = 0; i < type.Fields.Count; i++)//需要导出的字段计数
            {
                var field = type.ImplType.HierarchyFields[i];

                if (field.NeedExport())
                {
                    count++;
                    fieldIdx = i;
                }
            }

            //单字段输出值
            if (count == 1 && (dataType == "string" || dataType == "int"))
            {
                var f = type.Fields[fieldIdx];

                //根据datatype 强转
                if (dataType == "int")
                {
                    x.Append(int.Parse(f.ToString().Replace("\"", "")));
                }
                else if (dataType == "string")
                {
                    x.Append(f.ToString());
                }
            }
            else if (headType.StartsWith("json"))
            {
                WriteBean(x, type);
            }
        }

        void WriteBean(StringBuilder x, DBean bean)
        {
            var ss = new MemoryStream();
            var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
            {
                Indented = false,
                SkipValidation = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });

            bean.Apply(JsonIns, jsonWriter);//bean类型写入为json

            jsonWriter.Flush();

            var json = Encoding.UTF8.GetString(DataUtil.StreamToBytes(ss));
            x.Append(json.Replace("\"", "\"\""));
        }

        void AcceptList(List<DType> datas, StringBuilder x)
        {
            for (int i = 0; i < datas.Count; i++)
            {
                if (i == 0)
                {
                    x.Append('[');
                }
                var dtype = datas[i];

                dtype.Apply(this, x);

                if (i != (datas.Count - 1))
                {
                    x.Append(',');
                }

                if (i == datas.Count - 1)
                {
                    x.Append(']');
                }
            }
        }
        public void Accept(DArray type, StringBuilder x)
        {
            AcceptList(type.Datas, x);
        }

        public void Accept(DList type, StringBuilder x)
        {
            AcceptList(type.Datas, x);
        }

        public void Accept(DSet type, StringBuilder x)
        {
            AcceptList(type.Datas, x);
        }

        public void Accept(DMap type, StringBuilder x)
        {
            var count = type.Datas.Count;
            int index = 0;
            foreach (var d in type.Datas)
            {
                if (index == 0)
                {
                    x.Append('{');
                }
                d.Key.Apply(this, x);

                x.Append(':');

                d.Value.Apply(this, x);

                if (index != count - 1)
                {
                    x.Append(',');
                }
                else
                {
                    x.Append('}');
                }

                index++;
            }
        }
    }
}
