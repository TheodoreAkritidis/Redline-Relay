using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] float enemySpeed = 50f;
    //[SerializeField] float leftBoundary = 700f;
    //[SerializeField] float rightBoundary = 700f;

    public float direction = 1f;

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.left * enemySpeed * Time.deltaTime * direction);

        //if (transform.position.x < leftBoundary || transform.position.x > rightBoundary)
        //{
        //    Destroy(gameObject);
        //}
    }
}
