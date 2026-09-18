using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class itemMerging : MonoBehaviour
{
    public itemPickup itempickup;
    public bool isMerging;
    public Collider2D mergeHitbox;
    itemPickup mergingWithItem;
    public LayerMask layerMask;

    private void OnTriggerEnter2D(Collider2D other)
    {

        //item merging
        if (!isMerging && other.CompareTag("Item Merging") && itempickup && itempickup.Item && itempickup.count < itempickup.Item.maxStackSize)
        {
            mergingWithItem = other.transform.parent.GetComponent<itemPickup>();

            if (mergingWithItem.Item != itempickup.Item || (itempickup.localMotion == Vector2.zero && mergingWithItem.localMotion == Vector2.zero) || mergingWithItem.count + itempickup.count > mergingWithItem.Item.maxStackSize)
            {
                mergingWithItem = null;
                return;
            }

            // Find all potential items to merge with in the surrounding area
            Collider2D[] nearbyItems = Physics2D.OverlapCircleAll(transform.position, 1.5f, layerMask); // Adjust radius as necessary

            Collider2D closestItem = null;
            float closestDistance = Mathf.Infinity;

            // Find the closest item from all nearby items
            foreach (var item in nearbyItems)
            {
                if (item != mergeHitbox && item.CompareTag("Item Merging") && item.GetComponentInParent<itemPickup>().Item == itempickup.Item)
                {
                    float distance = Vector2.Distance(transform.position, item.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestItem = item;
                    }
                }
            }

            // If the current other collider isn't the closest item, return without merging
            if (closestItem != other)
            {
                return;
            }


            //more velocity merges with less - or higher position merges with lower if they are same velocity
            if (Mathf.Abs(itempickup.localMotion.magnitude) < Mathf.Abs(mergingWithItem.localMotion.magnitude) || (itempickup.transform.position.y < mergingWithItem.transform.position.y && Mathf.Abs(itempickup.localMotion.magnitude) == Mathf.Abs(mergingWithItem.localMotion.magnitude)))
            {
                isMerging = true;
                mergeHitbox.enabled = false;
                other.GetComponent<itemMerging>().isMerging = true;
                other.enabled = false;

                mergingWithItem.enabled = false;

                if (mergingWithItem.indicatorInstance)
                    Destroy(mergingWithItem.indicatorInstance, 0.5f);

                StartCoroutine(_goToOtherItem(mergingWithItem.transform, transform));
                itempickup.count += mergingWithItem.count;
                itempickup.sprite2.sprite = itempickup.Item.overworldSprite;
                StartCoroutine(_DestroyOther(mergingWithItem.gameObject));
            }
            else
            {
                isMerging = true;
                //mergeHitbox.enabled = false;
                itempickup.pickupHitbox.enabled = false;
            }


        }



    }
    private void OnDestroy()
    {
        if (isMerging && mergingWithItem)
        {
            itemMerging otherMerge = mergingWithItem.GetComponentInChildren<itemMerging>();
            mergingWithItem.enabled = true;
            otherMerge.isMerging = false;
            otherMerge.mergeHitbox.enabled = true;
            itempickup.pickupHitbox.enabled = true;
        }

    }


    IEnumerator _goToOtherItem(Transform Obj, Transform otherObj)
    {

        while (Obj != null && otherObj != null)
        {
            Obj.position = Vector2.MoveTowards(Obj.position, otherObj.position, 6f * Time.deltaTime);
            Obj.rotation = Quaternion.Lerp(Obj.rotation, otherObj.rotation, 14f * Time.deltaTime);

            yield return null;
        }

        isMerging = false;
        mergeHitbox.enabled = true;
    }

    IEnumerator _DestroyOther(GameObject mergeObj)
    {
        yield return new WaitForSeconds(0.15f);
        Destroy(mergeObj);
    }

}
