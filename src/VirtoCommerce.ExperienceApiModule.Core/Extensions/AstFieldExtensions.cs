using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure.Aliases;

namespace VirtoCommerce.ExperienceApiModule.Core.Extensions
{
    public static class AstFieldExtensions
    {
        public static string[] GetAllNodesPaths(this IResolveFieldContext context)
        {
            var currentRootType = context.ReturnType as IComplexGraphType;

            return context.SubFields.Values.SelectMany(x => x.GetAllTreeNodesPaths(currentRootType)).ToArray();
        }

        private static string[] GetAllTreeNodesPaths(this INode node, IComplexGraphType type, string path = null)
        {
            var rootPrefix = "__object.";
            var pathes = new List<string>();

            var aliasesMap = type.Fields
                .Where(x => x.HasAliasContainer())
                .ToDictionary(x => x.Name, x => x.GetAliasContainer());

            var isRoot = path == null;

            if (node is Field field)
            {
                var fieldName = field.Name;

                if (aliasesMap.TryGetValue(fieldName, out var aliaseContainer))
                {
                    foreach (var alias in aliaseContainer)
                    {
                        if (alias is RootAlias rootAlias)
                        {
                            pathes.Add(alias.Value);
                        }
                        if (alias is InnerAlias innerAlias)
                        {
                            fieldName = alias.Value;
                        }
                    }
                }

                path = isRoot ? $"{rootPrefix}{fieldName}" : string.Join(".", path, fieldName);
            }

            if (node.Children != null)
            {
                pathes.AddRange(node.Children.SelectMany(childNode => (childNode, node) switch
                {
                    (Field childField, Field rootField) => childNode.GetAllTreeNodesPaths(type.GetNestedGraphType(rootField.Name), path),

                    (SelectionSet subSelection, Field rootField)
                        when subSelection.Children.Any()
                            => childNode.Children.SelectMany(subSelect => subSelect.GetAllTreeNodesPaths(type.GetNestedGraphType(rootField.Name), path)),

                    _ => new[] { path }
                }));
            }
            else
            {
                pathes.Add(path);
            }

            return pathes.ToArray();
        }

        private static IComplexGraphType GetNestedGraphType(this IComplexGraphType type, string name)
        {
            var fieldType = type.Fields.FirstOrDefault(field => field.Name == name);
            if (fieldType.ResolvedType is ListGraphType listGraphType)
            {
                return listGraphType.ResolvedType as IComplexGraphType;
            }

            return fieldType.ResolvedType as IComplexGraphType;
        }

        public static IEnumerable<string> GetAllNodesPaths(this IEnumerable<Field> fields)
        {
            return fields.SelectMany(x => x.GetAllTreeNodesPaths()).Distinct();
        }

        private static IEnumerable<string> GetAllTreeNodesPaths(this INode node, string path = null)
        {
            if (node is Field field)
            {
                path = path != null ? string.Join(".", path, field.Name) : field.Name;
            }
            if (node.Children != null)
            {
                var childrenPaths = node.Children.SelectMany(n => n.GetAllTreeNodesPaths(path));
                foreach (var childPath in childrenPaths.Any() ? childrenPaths : new[] { path })
                {
                    yield return childPath;
                }
            }
            else
            {
                yield return path;
            }
        }
    }
}
