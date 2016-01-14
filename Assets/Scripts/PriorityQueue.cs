using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class PriorityQueue<K, T> where K : IComparable
{
    List<Element<K, T>> array;
    int nextElement;

    public PriorityQueue()
    {
        array = new List<Element<K, T>>();
        nextElement = 0;
    }

    public int Length
    {
        get { return nextElement; }
    }

    public T Min()
    {
        return array[0].value;
    }

    public T ExtractMin()
    {
        if (Length < 1)
            throw new Exception("Heap underflow");

        T min = array[0].value;
        array[0] = array[nextElement - 1];
        nextElement--;
        MinHeapify(0);

        return min;
    }

    public void DecreaseKey(K newKey, K oldKey)
    {
        if(newKey.CompareTo(oldKey) > 0)
            return;

        int index = Search(oldKey);

        if (index == -1)
            return;

        while (index > 0 && array[(index - 1) / 2].key.CompareTo(array[index]) > 0)
        {
            Element<K, T> tmp = array[index];
            array[index] = array[(index - 1) / 2];
            array[(index - 1) / 2] = tmp;

            index = (index - 1) / 2;
        }
    }

    public void Insert(K key, T value)
    {
        array.Add(new Element<K, T>(key, value));
        DecreaseKey(key, key);
        nextElement++;
    }

    public bool Contains(T value)
    {
        return array.FindIndex(el => el.value.Equals(value)) != -1;
    }

    void MinHeapify(int index)
    {
        int left = 2 * index + 1;
        int right = left + 1;
        
        int smallest;

        if (left < Length && array[left].key.CompareTo(array[index].key) < 0)
            smallest = left;
        else
            smallest = index;
        if (right < Length && array[right].key.CompareTo(array[smallest].key) < 0)
            smallest = right;

        if (smallest != index)
        {
            Element<K, T> tmp = array[index];
            array[index] = array[smallest];
            array[smallest] = tmp;

            MinHeapify(smallest);
        }
    }

    int Search(K key)
    {
        return array.FindIndex(el => el.key.CompareTo(key) == 0);
    }

    public override string ToString()
    {
        string result = "";

        for (int i = 0, howMany = 1; i < Length; i += howMany, howMany *= 2)
        {
            for (int j = 0; j < howMany; j++)
            {
                result += " " + array[i + j - 1].key.ToString();
            }
            result += "\n";
        }

        return result;
    }

    struct Element<U, V>
    {
        public U key;
        public V value;

        public Element(U key, V value)
        {
            this.key = key;
            this.value = value;
        }
    }
}
