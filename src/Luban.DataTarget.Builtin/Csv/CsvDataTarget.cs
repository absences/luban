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
            var s = JsonSerializer.Deserialize<CsvSet>(set);
            return s.headType;
        }

        public string GetDataType(string set)
        {
            var s = JsonSerializer.Deserialize<CsvSet>(set);
            return s.dataType;
        }

        void WriteBean(StringBuilder sb, DBean type)
        {
            sb.Append('{');

            var count = type.Fields.Count;

            for (int i = 0; i < count; i++)
            {
                var field = type.ImplType.HierarchyFields[i];

                if (field.NeedExport())
                {
                    sb.Append('"');
                    sb.Append('"');
                    sb.Append(field.Name);
                    sb.Append('"');
                    sb.Append('"');
                    sb.Append(':');

                    var f = type.Fields[i];
                    if (f != null)
                    {
                        WriteFieldData(sb, f, false);
                    }
                    else
                    {
                        sb.Append("null");
                    }

                }

                if (i != count - 1)
                {
                    sb.Append(',');
                }
            }
            sb.Append('}');
        }

        public void WriteCollectionHead(StringBuilder sb, TType elementType, bool array)
        {
            switch (elementType)
            {
                case TInt:
                    sb.Append("ints");
                    break;
                case TLong:
                    sb.Append("longs");
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
                    switch (Dlist[i])
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

                if (Dlist[i] is DBean bean)
                {
                    WriteBean(sb, bean);
                }
                else
                {
                    sb.Append(Dlist[i].ToString());
                }

                if (i != (Dlist.Count - 1))
                {
                    sb.Append(',');
                }

                if (i == Dlist.Count - 1)
                {
                    switch (Dlist[i])
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

        public void WriteMapData(StringBuilder sb, DMap map)
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

        public void WriteFieldData(StringBuilder sb, DType dType,bool root)
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
                WriteMapData(sb, map);
            }
            else if (dType is DString)
            {
                sb.Append('"');
                sb.Append(dType.ToString());
                sb.Append('"');
            }
            else if (dType is DEnum _enum)
            {
                sb.Append(_enum.StrValue);
            }
            else
            {
                if (dType is DBean _bean)
                {
                    var set = _bean.TType.DefBean.CsvSet;

                    var dataType = GetDataType(set);

                    var count = _bean.Fields.Count;

                    if (count == 1 && (dataType == "string" || dataType == "int"))
                    {
                        //单字段输出值
                        var f = _bean.Fields[0];

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
                    else
                    {
                        if (root)
                        {
                            sb.Append('"');
                        }
                       
                        WriteBean(sb, _bean);

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

                    else if (field.CType is TMap map)//todo
                    {
                        sb.Append(string.Format("json_{0}map", map.KeyType.TypeName));
                    }

                    else if (field.CType is TBean bean1)
                    {
                        var def = bean1.DefBean;

                        sb.Append(GetHeadType(def.CsvSet));
                    }
                    else if (field.CType is TEnum)
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
                        WriteFieldData(sb, dType,true);

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
