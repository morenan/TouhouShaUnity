using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouhouSha.Core;

public struct ShaPropertySync
{
    public ShaObject Owner;
    public string PropertyName;
    public int NewValue;
}

public struct PropertySync
{
    public object Owner;
    public string PropertyName;
    public object NewValue;
}

public struct StaticFieldSync
{
    public Type Type;
    public string FieldName;
    public object NewValue;

}

public struct CollectionSync
{
    public object Owner;
    public string CollectionName;
    public NotifyCollectionChangedEventArgs Event;
}