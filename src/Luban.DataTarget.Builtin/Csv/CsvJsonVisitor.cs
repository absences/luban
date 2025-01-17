using Luban.Datas;
using Luban.DataVisitors;
using Luban.Utils;
using System.Text.Json;

namespace Luban.DataExporter.Builtin.Csv
{
    internal class CsvJsonVisitor : IDataActionVisitor<Utf8JsonWriter>
    {
        public void Accept(DBool type, Utf8JsonWriter x)
        {
            x.WriteBooleanValue(type.Value);
        }

        public void Accept(DByte type, Utf8JsonWriter x)
        {
            x.WriteNumberValue(type.Value);
        }

        public void Accept(DShort type, Utf8JsonWriter x)
        {
            x.WriteNumberValue(type.Value);
        }

        public void Accept(DInt type, Utf8JsonWriter x)
        {
            x.WriteNumberValue(type.Value);
        }

        public void Accept(DLong type, Utf8JsonWriter x)
        {
            x.WriteNumberValue(type.Value);
        }

        public void Accept(DFloat type, Utf8JsonWriter x)
        {
            x.WriteNumberValue(type.Value);
        }

        public void Accept(DDouble type, Utf8JsonWriter x)
        {
            x.WriteNumberValue(type.Value);
        }

        public void Accept(DEnum type, Utf8JsonWriter x)
        {
            var item = type.Type.DefEnum.Items.First(f => f.IntValue == type.Value);
            x.WriteStringValue(item.Name);
        }

        public void Accept(DString type, Utf8JsonWriter x)
        {
            x.WriteStringValue(type.Value);
        }

        public void Accept(DDateTime type, Utf8JsonWriter x)
        {
            x.WriteNumberValue(type.UnixTimeOfCurrentContext());
        }

        public void Accept(DBean type, Utf8JsonWriter x)
        {
            x.WriteStartObject();

            var count = type.Fields.Count;

            for (int i = 0; i < count; i++)
            {
                var defFields = type.ImplType.HierarchyFields[i];

                if (defFields.NeedExport())
                {
                    var field = type.Fields[i];
                    bool hasElement = true;
                    if (defFields.CType.IsCollection)
                    {
                        if (field is DList list)
                        {
                            hasElement = list.Datas.Count > 0;
                        }
                        else if (field is DArray array)
                        {
                            hasElement = array.Datas.Count > 0;
                        }
                        else if (field is DMap map)
                        {
                            hasElement = map.Datas.Count > 0;
                        }
                    }
                    if (hasElement)
                    {
                        x.WritePropertyName(defFields.Name);

                        field.Apply(this, x);
                    }
                }
            }

            x.WriteEndObject();
        }

        void AcceptList(List<DType> datas, Utf8JsonWriter x)
        {
            x.WriteStartArray();
            foreach (var item in datas)
            {
                item.Apply(this, x);
            }
            x.WriteEndArray();
        }
        public void Accept(DArray type, Utf8JsonWriter x)
        {
            AcceptList(type.Datas, x);
        }

        public void Accept(DList type, Utf8JsonWriter x)
        {
            AcceptList(type.Datas, x);
        }

        public void Accept(DSet type, Utf8JsonWriter x)
        {
            AcceptList(type.Datas, x);
        }

        public void Accept(DMap type, Utf8JsonWriter x)
        {
            x.WriteStartObject();

            foreach (var pair in type.Datas)//字典以key作字段名
            {
                var key = pair.Key;
                if (key is DString str)
                {
                    x.WritePropertyName(str.Value);
                }
                else
                {
                    x.WritePropertyName(pair.Key.ToString());
                }

                pair.Value.Apply(this, x);
            }

            x.WriteEndObject();
        }
    }
}
