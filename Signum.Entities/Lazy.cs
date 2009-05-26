﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities.Properties; 

namespace Signum.Entities
{
    [Serializable]
    public class Lazy<T> : Lazy 
        where T : class, IIdentifiable
    {
        T entityOrNull;

        // Methods
        protected Lazy()
        {
        }

        public Lazy(int id)
            : base(typeof(T), id)
        {
        }

        public Lazy(Type runtimeType, int id)
            : base(runtimeType, id)
        {
        }

        public Lazy(T entidad)
            : base((IdentifiableEntity)(IIdentifiable)entidad)
        {   
        }

        public override IdentifiableEntity UntypedEntityOrNull
        {
            get { return (IdentifiableEntity)(object)EntityOrNull; }
            internal set { EntityOrNull = (T)(object)value; }
        }

        public T EntityOrNull
        {
            get { return entityOrNull; }
            internal set { entityOrNull = value; }
        }

        //public static bool operator ==(Lazy<T> lazy, T entity)
        //{
        //    throw new ApplicationException("For queries only");
        //}

        //public static bool operator !=(Lazy<T> lazy, T entity)
        //{
        //    throw new ApplicationException("For queries only");
        //}

        //public static bool operator ==(T entity, Lazy<T> lazy)
        //{
        //    throw new ApplicationException("For queries only");
        //}

        //public static bool operator !=(T entity, Lazy<T> lazy)
        //{
        //    throw new ApplicationException("For queries only");
        //}

        public bool Equals(Lazy<T> other)
        {
            if (other == null)
                return false;

            if (base.RuntimeType != other.RuntimeType)
                return false;

            if (EntityOrNull == null)
                return base.Id == other.IdOrNull;
            else
                return object.ReferenceEquals(this.EntityOrNull, other.EntityOrNull); 
        }

        public override bool Equals(object obj)
        {
            Lazy<T> casted = obj as Lazy<T>;
            return ((casted != null) && this.Equals(casted));
        }

        public override int GetHashCode()
        {
            if (this.EntityOrNull != null)
                return EntityOrNull.GetHashCode();
            return base.Id.GetHashCode() ^ base.RuntimeType.Name.GetHashCode();
        }
    }

    [Serializable]
    public abstract class Lazy : Modifiable 
    {
        Type runtimeType;
        int? id;
        string toStr;

        protected Lazy()
        {
        }

        public Lazy(Type runtimeType, int id)
        {
            if (runtimeType == null || !typeof(IdentifiableEntity).IsAssignableFrom(runtimeType))
                throw new ApplicationException(Resources.TypeIsNotSmallerThan.Formato(runtimeType, typeof(IIdentifiable)));

            this.runtimeType = runtimeType;
            this.id = id;
        }

        public Lazy(IdentifiableEntity entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException("entidad");

            this.runtimeType = entidad.GetType();
            this.ToStr = entidad.ToString();
            this.UntypedEntityOrNull = entidad;
            this.id = entidad.IdOrNull;
        }
      
        public int RefreshId()
        {
            if (UntypedEntityOrNull != null)
                id = UntypedEntityOrNull.Id;
            return id.Value; 
        }

        public Type RuntimeType
        {
            get { return runtimeType; }
        }

        public int Id
        {
            get
            {
                if (id == null)
                    throw new ApplicationException(Resources.TheLazyIsPointingToANewEntityAndHasNoIdYet);
                return id.Value;
            }
        }

        public int? IdOrNull
        {
            get { return id; }
        }

        public abstract IdentifiableEntity UntypedEntityOrNull
        {
            get;
            internal set;
        }

        public void SetEntity(IdentifiableEntity ei)
        {
            if (id == null)
                throw new ApplicationException(Resources.NewEntitiesAreNotAllowed); 

            if (id != ei.id || RuntimeType != ei.GetType())
                throw new ApplicationException(Resources.EntitiesDoNotMatch);

            this.UntypedEntityOrNull = ei;
        }

        public void ClearEntity()
        {
            if (id == null)
                throw new ApplicationException(Resources.RemovingEntityNotAllowedInNewLazies);

            this.UntypedEntityOrNull = null;
        }

        protected internal override void PreSaving()
        {
            UntypedEntityOrNull.TryDoC(e => e.PreSaving());
            if (UntypedEntityOrNull != null)
                toStr = UntypedEntityOrNull.ToStr;
            //Is better to have an old string than having nothing
        }

        public override bool SelfModified
        {
            get { return false; }
            internal set { }
        }

        public override string ToString()
        {
            if (this.UntypedEntityOrNull != null)
                return this.UntypedEntityOrNull.ToString();
            if (this.toStr != null)
                return this.toStr;
            return "{0}({1})".Formato(this.RuntimeType, this.id);
        }

        public string ToStringLong()
        {
            if (this.UntypedEntityOrNull == null)
                return "[({0}:{1}) ToStr:{2}]".Formato(this.runtimeType.Name, this.id, this.toStr);
            return "[{0}]".Formato(this.UntypedEntityOrNull);
        }

        public static Lazy Create(Type type, int id)
        {
            return (Lazy)Activator.CreateInstance(Reflector.GenerateLazy(type), type, id);
        }

        public static Lazy Create(Type type, Type runtimeType, int id)
        {
            return (Lazy)Activator.CreateInstance(Reflector.GenerateLazy(type), runtimeType, id);
        }

        public static Lazy Create(Type type, IdentifiableEntity entidad)
        {
            return (Lazy)Activator.CreateInstance(Reflector.GenerateLazy(type), entidad);
        }

        public string ToStr
        {
            get { return toStr; }
            internal set { toStr = value;  }
        }
    }


    public static class LazyUtils
    {
        public static Lazy<T> ToLazy<T>(this T entity) where T : class, IIdentifiable
        {
            if (entity.IsNew)
                throw new ApplicationException(Resources.ToLazyLightNotAllowedForNewEntities);

            var milazy = new Lazy<T>(entity);
            milazy.EntityOrNull = null;
            return milazy;
            
        }

        public static Lazy<T> ToLazy<T>(this T entidad, bool fat) where T : class, IIdentifiable
        {
            if (fat)
                return entidad.ToLazyFat();
            else
                return entidad.ToLazy(); 
        }

        public static Lazy<T> ToLazyFat<T>(this T entidad) where T : class, IIdentifiable
        {
            return new Lazy<T>(entidad);
        }
    }
}
