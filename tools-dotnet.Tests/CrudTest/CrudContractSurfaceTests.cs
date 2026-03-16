using System;
using System.Linq;
using System.Reflection;
using Shouldly;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Crud.Impl;
using tools_dotnet.Service;
using tools_dotnet.Service.Abstract;

namespace tools_dotnet.Tests.CrudTest
{
    [TestFixture]
    public class CrudContractSurfaceTests
    {
        private const string HardRemoveAsyncName = "HardRemoveAsync";

        [Test]
        public void NonSoftDeleteCrudContracts_ShouldNotExposeHardRemoveAsync()
        {
            foreach (var type in GetNonSoftDeleteCrudTypes())
            {
                HasPublicInstanceMethod(type, HardRemoveAsyncName)
                    .ShouldBeFalse($"{type.Name} should not expose HardRemoveAsync.");
            }
        }

        [Test]
        public void SoftDeleteCrudContracts_ShouldExposeHardRemoveAsync()
        {
            foreach (var type in GetSoftDeleteCrudTypes())
            {
                HasPublicInstanceMethod(type, HardRemoveAsyncName)
                    .ShouldBeTrue($"{type.Name} should expose HardRemoveAsync.");
            }
        }

        [Test]
        public void ConcurrentCrudContracts_ShouldNotExposeUntokenedRemoveAsync()
        {
            foreach (var type in GetConcurrentCrudTypes())
            {
                HasPublicInstanceMethod(type, "RemoveAsync", 2)
                    .ShouldBeFalse($"{type.Name} should not expose untokened RemoveAsync.");
            }
        }

        [Test]
        public void ConcurrentCrudContracts_ShouldNotExposeUntokenedUpdateAsync()
        {
            foreach (var type in GetConcurrentCrudTypesWithoutKeyWrapper())
            {
                HasPublicInstanceMethod(type, "UpdateAsync", 2)
                    .ShouldBeFalse($"{type.Name} should not expose untokened UpdateAsync.");
            }

            foreach (var type in GetConcurrentCrudTypesWithKeyWrapper())
            {
                HasPublicInstanceMethod(type, "UpdateAsync", 3)
                    .ShouldBeFalse($"{type.Name} should not expose untokened UpdateAsync.");
            }
        }

        private static Type[] GetNonSoftDeleteCrudTypes()
        {
            return
            [
                typeof(ICrudRepo<,>),
                typeof(IConcurrentCrudRepo<,,>),
                typeof(ICrudDtoRepo<,,,>),
                typeof(ICrudDtoRepo<,,>),
                typeof(IConcurrentCrudDtoRepo<,,,,>),
                typeof(IConcurrentCrudDtoRepo<,,,>),
                typeof(ICrudRepoWithKeyWrapper<,>),
                typeof(IConcurrentCrudRepoWithKeyWrapper<,,>),
                typeof(ICrudDtoRepoWithKeyWrapper<,,,>),
                typeof(ICrudDtoRepoWithKeyWrapper<,,>),
                typeof(IConcurrentCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(IConcurrentCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(ICrudService<,>),
                typeof(IConcurrentCrudService<,,>),
                typeof(ICrudServiceWithKeyWrapper<,,>),
                typeof(IConcurrentCrudServiceWithKeyWrapper<,,,>),
                typeof(BaseCrudRepo<,>),
                typeof(BaseConcurrentCrudRepo<,,>),
                typeof(BaseCrudDtoRepo<,,,>),
                typeof(BaseCrudDtoRepo<,,>),
                typeof(BaseConcurrentCrudDtoRepo<,,,,>),
                typeof(BaseConcurrentCrudDtoRepo<,,,>),
                typeof(BaseCrudRepoWithKeyWrapper<,>),
                typeof(BaseConcurrentCrudRepoWithKeyWrapper<,,>),
                typeof(BaseCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(BaseCrudDtoRepoWithKeyWrapper<,,>),
                typeof(BaseConcurrentCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(BaseConcurrentCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(BaseCrudService<,,,,>),
                typeof(BaseConcurrentCrudService<,,,,,>),
                typeof(BaseCrudServiceWithKeyWrapper<,,,,>),
                typeof(BaseConcurrentCrudServiceWithKeyWrapper<,,,,,>)
            ];
        }

        private static Type[] GetSoftDeleteCrudTypes()
        {
            return
            [
                typeof(ISoftDeleteCrudRepo<,>),
                typeof(IConcurrentSoftDeleteCrudRepo<,,>),
                typeof(ISoftDeleteCrudDtoRepo<,,,>),
                typeof(ISoftDeleteCrudDtoRepo<,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepo<,,,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepo<,,,>),
                typeof(ISoftDeleteCrudRepoWithKeyWrapper<,>),
                typeof(IConcurrentSoftDeleteCrudRepoWithKeyWrapper<,,>),
                typeof(ISoftDeleteCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(ISoftDeleteCrudDtoRepoWithKeyWrapper<,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(ISoftDeleteCrudService<,>),
                typeof(IConcurrentSoftDeleteCrudService<,,>),
                typeof(ISoftDeleteCrudServiceWithKeyWrapper<,,>),
                typeof(IConcurrentSoftDeleteCrudServiceWithKeyWrapper<,,,>),
                typeof(BaseSoftDeleteCrudRepo<,>),
                typeof(BaseConcurrentSoftDeleteCrudRepo<,,>),
                typeof(BaseSoftDeleteCrudDtoRepo<,,,>),
                typeof(BaseSoftDeleteCrudDtoRepo<,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepo<,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepo<,,,>),
                typeof(BaseSoftDeleteCrudRepoWithKeyWrapper<,>),
                typeof(BaseConcurrentSoftDeleteCrudRepoWithKeyWrapper<,,>),
                typeof(BaseSoftDeleteCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(BaseSoftDeleteCrudDtoRepoWithKeyWrapper<,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(BaseSoftDeleteCrudService<,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudService<,,,,,>),
                typeof(BaseSoftDeleteCrudServiceWithKeyWrapper<,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudServiceWithKeyWrapper<,,,,,>)
            ];
        }

        private static Type[] GetConcurrentCrudTypes()
        {
            return
            [
                typeof(IConcurrentCrudRepo<,,>),
                typeof(IConcurrentCrudDtoRepo<,,,,>),
                typeof(IConcurrentCrudDtoRepo<,,,>),
                typeof(IConcurrentSoftDeleteCrudRepo<,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepo<,,,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepo<,,,>),
                typeof(IConcurrentCrudRepoWithKeyWrapper<,,>),
                typeof(IConcurrentCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(IConcurrentCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(IConcurrentSoftDeleteCrudRepoWithKeyWrapper<,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(IConcurrentCrudService<,,>),
                typeof(IConcurrentSoftDeleteCrudService<,,>),
                typeof(IConcurrentCrudServiceWithKeyWrapper<,,,>),
                typeof(IConcurrentSoftDeleteCrudServiceWithKeyWrapper<,,,>),
                typeof(BaseConcurrentCrudRepo<,,>),
                typeof(BaseConcurrentCrudDtoRepo<,,,,>),
                typeof(BaseConcurrentCrudDtoRepo<,,,>),
                typeof(BaseConcurrentSoftDeleteCrudRepo<,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepo<,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepo<,,,>),
                typeof(BaseConcurrentCrudRepoWithKeyWrapper<,,>),
                typeof(BaseConcurrentCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(BaseConcurrentCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(BaseConcurrentSoftDeleteCrudRepoWithKeyWrapper<,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(BaseConcurrentCrudService<,,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudService<,,,,,>),
                typeof(BaseConcurrentCrudServiceWithKeyWrapper<,,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudServiceWithKeyWrapper<,,,,,>)
            ];
        }

        private static Type[] GetConcurrentCrudTypesWithoutKeyWrapper()
        {
            return
            [
                typeof(IConcurrentCrudRepo<,,>),
                typeof(IConcurrentCrudDtoRepo<,,,,>),
                typeof(IConcurrentCrudDtoRepo<,,,>),
                typeof(IConcurrentSoftDeleteCrudRepo<,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepo<,,,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepo<,,,>),
                typeof(IConcurrentCrudService<,,>),
                typeof(IConcurrentSoftDeleteCrudService<,,>),
                typeof(BaseConcurrentCrudRepo<,,>),
                typeof(BaseConcurrentCrudDtoRepo<,,,,>),
                typeof(BaseConcurrentCrudDtoRepo<,,,>),
                typeof(BaseConcurrentSoftDeleteCrudRepo<,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepo<,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepo<,,,>),
                typeof(BaseConcurrentCrudService<,,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudService<,,,,,>)
            ];
        }

        private static Type[] GetConcurrentCrudTypesWithKeyWrapper()
        {
            return
            [
                typeof(IConcurrentCrudRepoWithKeyWrapper<,,>),
                typeof(IConcurrentCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(IConcurrentCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(IConcurrentSoftDeleteCrudRepoWithKeyWrapper<,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(IConcurrentCrudServiceWithKeyWrapper<,,,>),
                typeof(IConcurrentSoftDeleteCrudServiceWithKeyWrapper<,,,>),
                typeof(BaseConcurrentCrudRepoWithKeyWrapper<,,>),
                typeof(BaseConcurrentCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(BaseConcurrentCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(BaseConcurrentSoftDeleteCrudRepoWithKeyWrapper<,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<,,,>),
                typeof(BaseConcurrentCrudServiceWithKeyWrapper<,,,,,>),
                typeof(BaseConcurrentSoftDeleteCrudServiceWithKeyWrapper<,,,,,>)
            ];
        }

        private static bool HasPublicInstanceMethod(Type type, string methodName)
        {
            return HasPublicInstanceMethod(
                type,
                method => method.Name == methodName
            );
        }

        private static bool HasPublicInstanceMethod(
            Type type,
            string methodName,
            int parameterCount
        )
        {
            return HasPublicInstanceMethod(
                type,
                method =>
                    method.Name == methodName && method.GetParameters().Length == parameterCount
            );
        }

        private static bool HasPublicInstanceMethod(
            Type type,
            Func<MethodInfo, bool> predicate
        )
        {
            return type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Concat(type.GetInterfaces().SelectMany(interfaceType => interfaceType.GetMethods()))
                .Any(predicate);
        }
    }
}
