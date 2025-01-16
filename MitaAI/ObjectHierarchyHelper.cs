using System.Text;
using UnityEngine;

public static class ObjectHierarchyHelper
{
    public static string GetObjectsInRadiusAsTree(GameObject origin, float radius, Transform requiredParent)
    {
        // Используем StringBuilder для построения строки
        StringBuilder treeBuilder = new StringBuilder();

        // Находим все объекты в радиусе
        Collider[] colliders = Physics.OverlapSphere(origin.transform.position, radius);

        // Итерация по объектам
        foreach (Collider collider in colliders)
        {
            Transform currentTransform = collider.transform;

            // Проверяем, имеет ли объект указанного родителя
            if (IsChildOfParent(currentTransform, requiredParent))
            {
                // Строим дерево имен
                AppendHierarchyToTree(currentTransform, requiredParent, treeBuilder, 0);
            }
        }

        return treeBuilder.ToString();
    }

    private static bool IsChildOfParent(Transform obj, Transform parent)
    {
        Transform current = obj;

        while (current != null)
        {
            if (current == parent)
                return true;

            current = current.parent;
        }

        return false;
    }

    private static void AppendHierarchyToTree(Transform current, Transform root, StringBuilder builder, int depth)
    {
        // Отступы для уровня вложенности
        builder.AppendLine(new string('-', depth * 2) + current.name);

        // Если это корень, заканчиваем построение
        if (current == root)
            return;

        // Рекурсивно добавляем родителя
        if (current.parent != null)
        {
            AppendHierarchyToTree(current.parent, root, builder, depth + 1);
        }
    }
}
