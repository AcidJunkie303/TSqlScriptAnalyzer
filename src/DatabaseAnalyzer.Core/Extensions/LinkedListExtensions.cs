namespace DatabaseAnalyzer.Core.Extensions;

public static class LinkedListExtensions
{
    public static void RemoveLast<T>(this LinkedList<T> linkedList, Predicate<T> predicate)
    {
        var node = linkedList.Last;
        while (node is not null)
        {
            var isMatch = predicate(node.Value);
            if (isMatch)
            {
                linkedList.Remove(node);
                return;
            }

            node = node.Previous;
        }
    }
}
