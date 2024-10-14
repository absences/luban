using Luban.Datas;
using Luban.DataTarget;
using Luban.Defs;
using Luban.Types;
using Luban.Utils;
using System.Text;

namespace Luban.DataExporter.Builtin.Csv
{
    /// <summary>
    /// 将配置表导出为csv
    /// </summary>

    [DataTarget("csv")]
    internal class CsvDataTarget : DataTargetBase
    {
        protected override string DefaultOutputFileExt => "csv";

        public override OutputFile ExportTable(DefTable table, List<Record> records)
        {
            StringBuilder sb = new StringBuilder();

            var fileds = records[0].Data.TType.DefBean.Fields;

            foreach (var field in fileds)
            {
                if (field.NeedExport())
                {
                    sb.Append('"');
                    sb.Append(field.Comment);
                    sb.Append('{');
                    sb.Append(field.Name);
                    sb.Append(',');

                    if (field.CType is TArray array)
                    {
                        switch (array.ElementType)
                        {
                            case TInt:
                                sb.Append("ints");
                                break;
                            case TLong:
                                sb.Append("longs");
                                break;
                            case TBean:
                                sb.Append("json_funcs");
                                break;
                        }
                    }
                    else if (field.CType is TMap map)
                    {
                        sb.Append(string.Format("json_{0}map", map.KeyType.TypeName));
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

            foreach (var record in records)
            {
                var dbean = record.Data;

                List<DType> data = record.Data.Fields;

                var defFields = dbean.ImplType.HierarchyFields;

                int index = 0;
                foreach (DType dType in data)
                {
                    var defField = defFields[index++];

                    if (!defField.NeedExport())
                    {
                        continue;
                    }

                    if (dType is DArray array)
                    {
                        sb.Append('"');

                        var count = array.Datas.Count;
                        for (int i = 0; i < count; i++)
                        {
                            if (i == 0)
                            {
                                switch (array.Datas[i])
                                {
                                    case DInt:
                                    case DLong:
                                        sb.Append('{');
                                        break;
                                    case DBean:
                                        sb.Append('[');
                                        break;
                                }
                            }

                            if (array.Datas[i] is DBean type)
                            {
                                //if (type.Type.IsAbstractType)
                                //{
                                //    sb.Append($"{{ _name:\"{type.ImplType.Name}\",");
                                //}
                                //else
                                {
                                    sb.Append('{');
                                }

                                var c = type.Fields.Count;

                                for (int j = 0; j < c; j++)
                                {
                                    var hf = type.ImplType.HierarchyFields[j];
                                    sb.Append('"');
                                    sb.Append('"');
                                    sb.Append(hf.Name);
                                    sb.Append('"');
                                    sb.Append('"');
                                    sb.Append(':');
                                    var f = type.Fields[j];
                                    if (f != null)
                                    {
                                        sb.Append(f.ToString());
                                    }
                                    else
                                    {
                                        sb.Append("null");
                                    }
                                    if (j != c - 1)
                                    {
                                        sb.Append(',');
                                    }

                                }
                                sb.Append('}');
                            }
                            else
                            {
                                sb.Append(array.Datas[i].ToString());
                            }

                            if (i != (count - 1))
                            {
                                sb.Append(',');
                            }

                            if (i == count - 1)
                            {
                                switch (array.Datas[i])
                                {
                                    case DInt:
                                    case DLong:
                                        sb.Append('}');
                                        break;
                                    case DBean:
                                        sb.Append(']');
                                        break;
                                }
                            }
                        }

                        sb.Append('"');
                    }
                    else if (dType is DMap map)
                    {
                        sb.Append('{');
                        var count = map.Datas.Count;
                        int idx = 0;
                        foreach (var (k, v) in map.Datas)
                        {
                            sb.Append(k);
                            sb.Append(':');
                            sb.Append(v);

                            if (idx != count - 1)
                            {
                                sb.Append(',');
                            }

                            idx++;
                        }

                        sb.Append('}');
                    }
                    else if (dType is DString)
                    {
                        sb.Append(dType.ToString().Replace("\\", "\""));
                    }
                    else
                    {
                        sb.Append(dType.ToString());
                    }
                    sb.Append(',');
                }
                sb.Append('\n');
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            var content = new byte[bytes.Length + 3];
            content[0] = 0xef;
            content[1] = 0xbb;
            content[2] = 0xbf;

            Buffer.BlockCopy(bytes, 0, content, 3, bytes.Length);

            CreateOutputFile($"{table.OutputDataFile}.{OutputFileExt}", Encoding.UTF8.GetString(content));
        }
    }
}
