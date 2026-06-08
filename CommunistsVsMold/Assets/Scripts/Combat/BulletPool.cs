using System.Collections.Generic;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Простой пул-синглтон снарядов. Оружие берёт пулю через Get(),
    /// сама пуля возвращается через Return() при попадании/исчерпании дальности.
    /// </summary>
    public class BulletPool : MonoBehaviour
    {
        public static BulletPool Instance { get; private set; }

        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private int prewarm = 32;

        private readonly Queue<Bullet> pool = new Queue<Bullet>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            for (int i = 0; i < prewarm; i++)
            {
                Bullet b = Instantiate(bulletPrefab, transform);
                b.gameObject.SetActive(false);
                pool.Enqueue(b);
            }
        }

        /// <summary>Достать пулю из пула (или создать новую), активировать и вернуть.</summary>
        public Bullet Get()
        {
            Bullet b = pool.Count > 0 ? pool.Dequeue() : Instantiate(bulletPrefab, transform);
            b.gameObject.SetActive(true);
            return b;
        }

        /// <summary>Вернуть пулю в пул и деактивировать.</summary>
        public void Return(Bullet b)
        {
            b.gameObject.SetActive(false);
            pool.Enqueue(b);
        }
    }
}
