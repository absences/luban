using Luban.Datas;
using Luban.DataTarget;
using Luban.Defs;
using Luban.Types;
using Luban.Utils;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Luban.DataExporter.Builtin.Csv
{
    /// <summary>
    /// 将配置表导出为csv
    /// </summary>

    [DataTarget("csv")]
    internal class CsvDataTarget : DataTargetBase
    {
        protected override string DefaultOutputFileExt => "csv";

        public class CsvSet
        {
            public string headType { get; set; }
            public string dataType { get; set; }
        }

        public string GetHeadType(string set)
        {
            if (string.IsNullOrEmpty(set))
            {
                return string.Empty;
            }
            var s = JsonSerializer.Deserialize<CsvSet>(set);
            return s.headType;
        }

        public string GetDataType(string set)
        {
            if (string.IsNullOrEmpty(set))
            {
                return string.Empty;
            }
            var s = JsonSerializer.Deserialize<CsvSet>(set);
            return s.dataType;
        }

        void WriteBean(StringBuilder sb, DBean bean)
        {
            var ss = new MemoryStream();
            var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
            {
                Indented = false,
                SkipValidation = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });

            AcceptBean(bean, jsonWriter);

            jsonWriter.Flush();

            var json = Encoding.UTF8.GetString(DataUtil.StreamToBytes(ss));
            sb.Append(json.Replace("\"", "\"\""));
        }

        void AcceptType(DType field, Utf8JsonWriter x)
        {
            switch (field)
            {
                case DBool type:
                    x.WriteBooleanValue(type.Value);
                    break;
                case DByte type:
                    x.WriteNumberValue(type.Value);
                    break;
                case DShort type:
                    x.WriteNumberValue(type.Value);
                    break;
                case DInt type:
                    x.WriteNumberValue(type.Value);
                    break;
                case DLong type:
                    x.WriteNumberValue(type.Value);
                    break;
                case DFloat type:
                    x.WriteNumberValue(type.Value);
                    break;
                case DDouble type:
                    x.WriteNumberValue(type.Value);
                    break;
                case DString type:
                    x.WriteStringValue(type.Value);
                    break;
                case DBean type:
                    AcceptBean(type, x);
                    break;
                case DEnum type:
                    x.WriteStringValue(type.StrValue);
                    break;
            }
        }


        void AcceptBean(DBean bean, Utf8JsonWriter x)
        {
            x.WriteStartObject();

            var count = bean.Fields.Count;

            for (int i = 0; i < count; i++)
            {
                var defFields = bean.ImplType.HierarchyFields[i];

                if (defFields.NeedExport())
                {
                    x.WritePropertyName(defFields.Name);

                    var field = bean.Fields[i];

                    AcceptType(field, x);
                   
                }
            }

            x.WriteEndObject();
        }

        /// <summary>
        /// 容器类型写入
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="elementType"></param>
        /// <param name="array"></param>
        public void WriteCollectionHead(StringBuilder sb, TType elementType, bool array)
        {
            switch (elementType)
            {
                case TInt:
                case TLong:
                case TDouble:
                case TBool:
                case TByte:
                case TShort:
                case TFloat:
                    sb.Append(elementType.TypeName);
                    sb.Append('s');
                    break;
                case TString:
                    sb.Append("strs");
                    break;
                case TBean bean:
                {
                    var def = bean.DefBean;

                    sb.Append(GetHeadType(def.CsvSet) + (array ? "Ary" : "s"));
                }
                break;
            }
        }

        /// <summary>
        /// 写入容器数据
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="type"></param>
        public void WriteCollectionData(StringBuilder sb, DType type)
        {
            sb.Append('"');
            List<DType> Dlist = null;

            if (type is DList list)
            {
                Dlist = list.Datas;
            }
            else if (type is DArray array)
            {
                Dlist = array.Datas;
            }

            for (int i = 0; i < Dlist.Count; i++)
            {
                if (i == 0)
                {
                    sb.Append('[');
                }
                var dtype = Dlist[i];

                WriteFieldData(sb, dtype, false);


                if (i != (Dlist.Count - 1))
                {
                    sb.Append(',');
                }

                if (i == Dlist.Count - 1)
                {
                    sb.Append(']');
                }
            }

            sb.Append('"');
        }

        public void WriteMapData(StringBuilder sb, DMap map)
        {
            var count = map.Datas.Count;
  
            var keyType= map.Type.KeyType;
            var valueType= map.Type.ValueType;

            var ss = new MemoryStream();
            var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
            {
                Indented = false,
                SkipValidation = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });

            AcceptMap(map, jsonWriter);

            jsonWriter.Flush();

            var json = Encoding.UTF8.GetString(DataUtil.StreamToBytes(ss));
            sb.Append(json.Replace("\"", "\"\""));

        }
        void AcceptMap(DMap map, Utf8JsonWriter x)
        {
            x.WriteStartArray();
            foreach (var d in map.Datas)
            {
                x.WriteStartArray();

                AcceptType(d.Key,x);

                AcceptType(d.Value,x);

                x.WriteEndArray();
            }
            x.WriteEndArray();
        }

        public void WriteFieldData(StringBuilder sb, DType dType, bool root)
        {
            if (dType is DArray)
            {
                WriteCollectionData(sb, dType);
            }
            else if (dType is DList)
            {
                WriteCollectionData(sb, dType);
            }
            else if (dType is DMap map)
            {
                if (root)
                {
                    sb.Append('"');
                }
                WriteMapData(sb, map);
                if (root)
                {
                    sb.Append('"');
                }
            }
            else if (dType is DString str)
            {
                sb.Append(DataUtil.EscapeString(str.Value));
            }
            else if (dType is DEnum _enum)
            {
                sb.Append(_enum.StrValue);
            }
            else if (dType is DBean bean)
            {
                var set = bean.TType.DefBean.CsvSet;

                var headType = GetHeadType(set);
                var dataType = GetDataType(set);

                var count = 0;
                var fieldIdx = 0;

                for (int i = 0; i < bean.Fields.Count; i++)//需要导出的字段计数
                {
                    var field = bean.ImplType.HierarchyFields[i];

                    if (field.NeedExport())
                    {
                        count++;
                        fieldIdx = i;
                    }
                }

                //单字段输出值
                if (count == 1 && (dataType == "string" || dataType == "int"))
                {
                    var f = bean.Fields[fieldIdx];

                    //根据datatype 强转
                    if (dataType == "int")
                    {
                        sb.Append(int.Parse(f.ToString().Replace("\"", "")));
                    }
                    else if (dataType == "string")
                    {
                        sb.Append(f.ToString());
                    }
                }
                else if (headType.StartsWith("json"))
                {
                    if (root)
                    {
                        sb.Append('"');
                    }
                    WriteBean(sb, bean);

                    if (root)
                    {
                        sb.Append('"');
                    }
                }
            }
            else
            {
                sb.Append(dType.ToString());
            }

        }


        public override OutputFile ExportTable(DefTable table, List<Record> records)
        {
            StringBuilder sb = new StringBuilder();

            var fileds = records[0].Data.TType.DefBean.Fields;
            //=================write type
            foreach (var field in fileds)
            {
                if (field.NeedExport())
                {
                    sb.Append('"');
                    sb.Append(field.Comment);

                    sb.Append('{');
                    sb.Append(field.Name.Substring(0, 1).ToLower());//首字母小写
                    sb.Append(field.Name.Substring(1));

                    sb.Append(',');

                    if (field.CType is TArray array)
                    {
                        WriteCollectionHead(sb, array.ElementType, true);
                    }
                    else if (field.CType is TList list)
                    {
                        WriteCollectionHead(sb, list.ElementType, false);
                    }

                    else if (field.CType is TMap map)
                    {
                        if (map.Tags.TryGetValue("headType", out var headType))
                        {
                            sb.Append(headType);
                        }
                        else
                        {
                            throw new Exception($"map headType为空");
                        }
                    }

                    else if (field.CType is TBean bean1)
                    {
                        var def = bean1.DefBean;

                        sb.Append(GetHeadType(def.CsvSet));
                    }
                    else if (field.CType is TEnum or TString)
                    {
                        sb.Append("str");
                    }
                    else
                    {
                        sb.Append(field.Type);
                    }

                    sb.Append('}');
                    sb.Append('"');
                    sb.Append(',');
                }
            }
            sb.Append('\n');
            //=================write data
            foreach (var record in records)
            {
                var dbean = record.Data;

                List<DType> data = record.Data.Fields;

                var defFields = dbean.ImplType.HierarchyFields;

                int index = 0;
                foreach (DType dType in data)
                {
                    var defField = defFields[index++];

                    if (defField.NeedExport())
                    {
                        WriteFieldData(sb, dType, true);

                        sb.Append(',');
                    }
                }


                sb.Append('\n');
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            var content = new byte[bytes.Length + 3];
            //bom
            content[0] = 0xef;
            content[1] = 0xbb;
            content[2] = 0xbf;

            Buffer.BlockCopy(bytes, 0, content, 3, bytes.Length);

            return CreateOutputFile($"{table.OutputDataFile}.{OutputFileExt}", Encoding.UTF8.GetString(content));
        }
    }
}
