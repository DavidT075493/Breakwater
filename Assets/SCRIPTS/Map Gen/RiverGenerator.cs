using UnityEngine;
using UnityEngine.Tilemaps;

public class RiverGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public AdvancedRuleTile tile;
    public int radius;
    int startRadius;
    public int numberOfCircles;
    public float turnChance;
    public float turnChanceRight;
    public float turnChanceDown;
    public float turnChanceLeft;
    public float turnChanceUp;

    Vector3Int currentPosition;
    Vector2Int direction;

    public Transform startPoint;
    public Vector2 riversMinMax;

    public void GenerateRiver()
    {
        startRadius = radius;

        for (int i = Mathf.RoundToInt(Random.Range(riversMinMax.x, riversMinMax.y)); i > 0; i--)
        {
            int randomStart = Random.Range(0, 4);
            //randomStart = 2;

            switch (randomStart)
            {
                //left
                case 0:
                    startPoint.transform.position = new Vector3Int(-MapGenerator.mapChunkSize / 2, Random.Range(-MapGenerator.mapChunkSize / 2, MapGenerator.mapChunkSize / 2));

                    if (startPoint.transform.position.y > 0)
                    {
                        turnChanceDown = Random.Range(0.6f, 0.95f);
                        turnChanceUp = 0.4f;
                    }
                    else
                    {
                        turnChanceUp = Random.Range(0.6f, 0.95f);
                        turnChanceDown = 0.4f;
                    }

                    break;
                //right
                case 1:
                    startPoint.transform.position = new Vector3Int(MapGenerator.mapChunkSize / 2, Random.Range(-MapGenerator.mapChunkSize / 2, MapGenerator.mapChunkSize / 2));

                    if (startPoint.transform.position.y > 0)
                    {
                        turnChanceDown = Random.Range(0.6f, 0.95f);
                        turnChanceUp = 0.4f;
                    }
                    else
                    {
                        turnChanceUp = Random.Range(0.6f, 0.95f);
                        turnChanceDown = 0.4f;
                    }

                    break;
                //up
                case 2:
                    startPoint.transform.position = new Vector3Int(Random.Range(-MapGenerator.mapChunkSize / 2, MapGenerator.mapChunkSize / 2), MapGenerator.mapChunkSize / 2);
                    
                    if(startPoint.transform.position.x > 0)
                    {
                        turnChanceLeft = Random.Range(0.6f, 0.95f);
                        turnChanceRight = 0.4f;
                    }
                    else
                    {
                        turnChanceRight = Random.Range(0.6f, 0.95f);
                        turnChanceLeft = 0.4f;
                    }

                    break;
                //down
                case 3:
                    startPoint.transform.position = new Vector3Int(Random.Range(-MapGenerator.mapChunkSize / 2, MapGenerator.mapChunkSize / 2), -MapGenerator.mapChunkSize / 2);

                    if (startPoint.transform.position.x > 0)
                    {
                        turnChanceLeft = Random.Range(0.6f, 0.95f);
                        turnChanceRight = 0.4f;
                    }
                    else
                    {
                        turnChanceRight = Random.Range(0.6f, 0.95f);
                        turnChanceLeft = 0.4f;
                    }

                    break;


            }


            currentPosition = new Vector3Int(Mathf.RoundToInt(-startPoint.position.x), Mathf.RoundToInt(-startPoint.position.y), 0);
            direction = Vector2Int.right;

            for (int i2 = 0; i2 < numberOfCircles; i2++)
            {
                DrawCircle(currentPosition);

                currentPosition += (Vector3Int)direction;

                if (Random.value < turnChance)
                {
                    if (direction == Vector2Int.right)
                    {
                        if (Random.value < turnChanceRight)
                        {
                            if (Random.Range(0, 2) == 0)
                            {
                                direction = RotateVector(direction, -90);
                            }
                            else
                            {
                                direction = RotateVector(direction, 90);
                            }
                        }
                    }
                    else if (direction == Vector2Int.down)
                    {
                        if (Random.value < turnChanceDown)
                        {
                            if (Random.Range(0, 2) == 0)
                            {
                                direction = RotateVector(direction, -90);
                            }
                            else
                            {
                                direction = RotateVector(direction, 90);
                            }
                        }
                    }
                    else if (direction == Vector2Int.left)
                    {
                        if (Random.value < turnChanceLeft)
                        {
                            if (Random.Range(0, 2) == 0)
                            {
                                direction = RotateVector(direction, -90);
                            }
                            else
                            {
                                direction = RotateVector(direction, 90);
                            }
                        }
                    }
                    else if (direction == Vector2Int.up)
                    {
                        if (Random.value < turnChanceUp)
                        {
                            if (Random.Range(0, 2) == 0)
                            {
                                direction = RotateVector(direction, -90);
                            }
                            else
                            {
                                direction = RotateVector(direction, 90);
                            }
                        }
                    }
                }

                //big turns
                if (Random.Range(0, 600) == 0 && i2 > 500)
                {
                    //up down
                    if (randomStart == 2 || randomStart == 3)
                    {
                        turnChanceRight = 1 - turnChanceRight;
                        turnChanceLeft = 1 - turnChanceLeft;
                    }
                    //left right
                    else
                    {
                        turnChanceUp = 1 - turnChanceUp;
                        turnChanceDown = 1 - turnChanceDown;

                    }
                }

                //change width
                if (Random.Range(0, 200) == 0)
                {
                    if(radius < startRadius + 3)
                    {
                        radius++;
                    }
                    if (radius > startRadius - 3)
                    {
                        radius--;
                    }
                }

            }

            radius = startRadius;
        }


    }
    void DrawCircle(Vector3Int center)
    {
        int diameter = radius * 2;

        for (int i = 0; i <= diameter; i++)
        {
            for (int j = 0; j <= diameter; j++)
            {
                int x = i - radius;
                int y = j - radius;

                if (x * x + y * y <= radius * radius)
                {
                    Vector3Int position = new Vector3Int(center.x + x, center.y + y, center.z);
                    tilemap.SetTile(position, tile);
                }
            }
        }
    }

    public Vector2Int RotateVector(Vector2Int v, float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        int _x = Mathf.RoundToInt(v.x * Mathf.Cos(radian) - v.y * Mathf.Sin(radian));
        int _y = Mathf.RoundToInt(v.x * Mathf.Sin(radian) + v.y * Mathf.Cos(radian));
        return new Vector2Int(_x, _y);
    }
    public void Clear()
    {
        tilemap.ClearAllTiles();
    }
}
