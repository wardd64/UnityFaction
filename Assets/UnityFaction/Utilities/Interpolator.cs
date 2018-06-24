using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour {

    /// <summary>
    /// Flat Cubic Bezier Easing Funcion: 
    /// Maps given float r from [0,1] to [0,1] in a smooth way, with flat, 
    /// derivative 0 tangents at the beginning and ending.
    /// easeIn and easeOut control the easing strengths at both sides
    /// e.g. setting ease to (1,1) leads to a sudden jump in the middle, 
    /// while (0,0) yields a linear interpolation
    /// </summary>
    public static float FCBEF(float r, float easeIn, float easeOut) {
        r = Mathf.Clamp01(r);

        //makes result more visually appealing
        easeIn = Mathf.Sqrt(easeIn);
        easeOut = Mathf.Sqrt(easeOut);

        //convert to bezier control point coordinates
        float a = Mathf.Clamp01(easeIn);
        float b = 1f - Mathf.Clamp01(easeOut);

        // Find value of bezier parameter that yields x value == r.
        float t;
        if(r <= 0f)
            t = 0f;
        else if(r >= 1f)
            t = 1f;
        else {
            //invert x-axis bezier function
            //TODO invert with newton method for improved performance
            t = .5f;
            float corr = 0.25f;
            for(int i = 0; i < ITERATIONS; i++) {
                if(Bez01(t, a, b) < r)
                    t += corr;
                else
                    t -= corr;
                corr /= 2f;
            }
        }
        
        //use bezier parameter to find y-axis output
        return Bez01(t, 0f, 1f);
    }

    const int ITERATIONS = 25;

    /// <summary>
    /// Returns 1 coordinate of a cubic bezier curve that extends from 0 to 1.
    /// a and b correspond to the relevant coordinate of the middle 2 control points, 
    /// while t is the regular bezier parameter.
    /// The output is restrained to [0,1] as long as all input values are.
    /// </summary>
    public static float Bez01(float t, float a, float b) {
        float at = 1f - t;
        return t * ( 3f*at * (at*a + t*b) + t*t );
    }
}
