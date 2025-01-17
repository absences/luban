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

        protected virtual CsvDataVisitor ImplCsvDataVisitor => CsvDataVisitor.Ins;

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

                    sb.Append(CsvSet.GetHeadType(def.CsvSet) + (array ? "Ary" : "s"));
                }
                break;
            }
        }

        void WriteTableHead(List<DefField> fileds, StringBuilder sb)
        {
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

                        sb.Append(CsvSet.GetHeadType(def.CsvSet));
                    }
                    else if (field.CType is TEnum or TString)
                    {
                        sb.Append("str");
                    }
                    else
                    {
                        sb.Append(field.CType.TypeName);
                    }

                    sb.Append('}');
                    sb.Append('"');
                    sb.Append(',');
                }
            }
        }
        public void WriteRecords(List<Record> records, StringBuilder sb, CsvDataVisitor csvDataVisitor)
        {
            foreach (var record in records)
            {
                var dbean = record.Data;

                List<DType> data = record.Data.Fields;

                var defFields = dbean.ImplType.HierarchyFields;

                int index = 0;

                foreach (DType dType in data)
                {
                    var defField = defFields[index++];

                    if (dType != null && defField.NeedExport())
                    {
                        bool needWarp = defField.CType.IsCollection;

                        if (dType is DBean bean)
                        {
                            var set = bean.TType.DefBean.CsvSet;

                            var headType = CsvSet.GetHeadType(set);

                            if (headType.StartsWith("json"))
                            {
                                needWarp = true;
                            }
                        }

                        if (needWarp)
                        {
                            sb.Append('"');
                        }
                        dType.Apply(csvDataVisitor, sb);

                        if (needWarp)
                        {
                            sb.Append('"');
                        }

                        sb.Append(',');
                    }
                }

                sb.Append('\n');
            }
        }

        public override OutputFile ExportTable(DefTable table, List<Record> records)
        {
            StringBuilder sb = new();

            //write head
            WriteTableHead(table.ValueTType.DefBean.Fields, sb);

            sb.Append('\n');

            //write data
            WriteRecords(records, sb, ImplCsvDataVisitor);

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            var content = new byte[bytes.Length + 3];
            //bom
            content[0] = 0xef;
            content[1] = 0xbb;
            content[2] = 0xbf;

            Buffer.BlockCopy(bytes, 0, content, 3, bytes.Length);
            return CreateOutputFile($"{table.OutputDataFile}.{OutputFileExt}", content);
        }
    }
}
