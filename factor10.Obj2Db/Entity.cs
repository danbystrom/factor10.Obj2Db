using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public enum TypeOfEntity
    {
        Class,
        PlainField,
        IEnumerable,
    }

    public class Entity
    {
        public string Name;

        public LinkedFieldInfo FieldInfo { get; internal set; }
        public TypeOfEntity TypeOfEntity { get; internal set; }

        public List<Entity> Fields = new List<Entity>();

        private Entity()
        {
        }

        public bool IsField => !Fields.Any();

        public static Entity Begin(string name = null)
        {
            return new Entity {Name = name};
        }

        public Entity Add(Entity entity)
        {
            Fields.Add(entity);
            return this;
        }

        public static implicit operator Entity(string name)
        {
            return new Entity {Name = name};
        }

    }

    public class ProcessedEntity
    {
        public readonly string Name;

        public readonly Table Table;

        public LinkedFieldInfo FieldInfo { get; internal set; }
        public TypeOfEntity TypeOfEntity { get; internal set; }
        public string ExternalName => Name;

        public readonly List<ProcessedEntity> Fields = new List<ProcessedEntity>();
        public readonly List<ProcessedEntity> Lists = new List<ProcessedEntity>();

        private ProcessedEntity()
        {
            
        }

        public ProcessedEntity(Type type, Entity entity)
        {
            Name = entity.Name;

            if (Name != null)
            {
                FieldInfo = new LinkedFieldInfo(type, Name);
                var ft = FieldInfo.FieldInfo.FieldType;
                var ienum = ft != typeof (string)
                    ? ft.GetInterfaces().SingleOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof (IEnumerable<>))
                    : null;

                if (ienum != null)
                {
                    TypeOfEntity = TypeOfEntity.IEnumerable;
                    type = ienum.GetGenericArguments()[0];
                    if (entity.IsField)
                        Fields.Add(new ProcessedEntity {FieldInfo = LinkedFieldInfo.Null(type) });
                }
                else if (entity.IsField)
                    TypeOfEntity = TypeOfEntity.PlainField;
                else
                    TypeOfEntity = TypeOfEntity.Class;
            }

            foreach (var subEntity in entity.Fields)
            {
                var pe = new ProcessedEntity(type, subEntity);
                (pe.TypeOfEntity == TypeOfEntity.IEnumerable ? Lists : Fields).Add(pe);
            }

            if (TypeOfEntity != TypeOfEntity.PlainField)
                Table = new Table(this, false);
        }

        public object[] GetRow(object obj)
        {
            if (TypeOfEntity == TypeOfEntity.PlainField)
                return new[] { FieldInfo.GetValue(obj) };
            return Fields.Select(_ => _.FieldInfo.GetValue(obj)).ToArray();
        }

        public IEnumerable<ProcessedEntity> GetAll()
        {
            yield return this;
            foreach (var x in Fields.SelectMany(_ => _.GetAll()))
                yield return x;
            foreach (var x in Lists.SelectMany(_ => _.GetAll()))
                yield return x;
        }
    }

}