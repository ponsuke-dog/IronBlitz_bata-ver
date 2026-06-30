using System.Collections;
using UnityEngine;

public class UIAnimationController : MonoBehaviour
{
    // 動かす対象,終わりの位置,時間,遅延時間
    public Coroutine Move(RectTransform target, Vector2 endpos, float duration = 1f,float delay = 0f ,EasingType type = EasingType.Linear)
    {
        return StartCoroutine(MoveCorutine(target,endpos,duration,delay,type));
    }

    IEnumerator MoveCorutine(RectTransform target,Vector2 endpos,float duration, float delay ,EasingType type)
    {
        yield return new WaitForSeconds(delay);

        Vector2 startPos = target.anchoredPosition;
        float time = 0f;

        while(time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = EasingFunction.Evaluate(type,t);

            target.anchoredPosition = Vector2.Lerp(startPos, endpos, t);

            yield return null;
        }
        target.anchoredPosition = endpos;
    }

    // 動かす対象,終わりの位置,時間
    public IEnumerator Scale(
     RectTransform target,
     Vector3 endscale,
     float duration = 1,
     float delay = 0f,
     EasingType type = EasingType.Linear)
    {
        yield return ScaleCorutine(
            target,
            endscale,
            duration,
            delay,
            type);
    }

    IEnumerator ScaleCorutine(RectTransform target,Vector3 endscale,float duration,float delay,EasingType type)
    {
        yield return new WaitForSeconds(delay);

        Vector3 startScale = target.localScale;
        float time = 0f;

        while(time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = EasingFunction.Evaluate(type, t);

            target.localScale = Vector3.Lerp(startScale, endscale, t);

            yield return null;
        }
        target.localScale = endscale;
        Debug.Log("Scale終了");
    }
}
