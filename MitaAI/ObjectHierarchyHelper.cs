using System.Text;
using UnityEngine;
using Il2CppSystem.Collections.Generic;


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
                AppendHierarchyToTree(currentTransform, treeBuilder, 0, origin.transform.position);
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

    private static void AppendHierarchyToTree(Transform current, StringBuilder builder, int depth, Vector3 from)
    {
        string distance = $" {Vector2.Distance(current.transform.position, from):F2} м";

        // Добавляем текущий объект с отступами
        builder.AppendLine(new string('-', depth) + current.name + distance);

        // Рекурсивно добавляем всех детей
        int childCount = current.childCount;
        for (int i = 0; i < childCount; i++)
        {
            // Получаем дочерний объект по индексу
            Transform child = current.GetChild(i);

            // Рекурсивно добавляем ребенка
            AppendHierarchyToTree(child, builder, depth + 1,from);
        }
    }
}
