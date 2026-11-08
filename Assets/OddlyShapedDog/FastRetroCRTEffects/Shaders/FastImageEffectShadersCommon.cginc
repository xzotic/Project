// returns a rectangle contained in the main texture UV space,
// with the desired aspect ratio
float4 getAspectRatiodRect(float desiredAspectRatio, float actualAspectRatio) {
    float width, height;
    if (actualAspectRatio > desiredAspectRatio) {
        width = actualAspectRatio / desiredAspectRatio;
        height = 1.0;
    }
    else {
        width = 1.0;
        height = actualAspectRatio / desiredAspectRatio;
    }
                
    float x = (1.0 - width) / 2.0;
    float y = (1.0 - height) / 2.0;
    return float4(
        x,
        y,
        1 / width,
        1 / height
    );
}

// maps a set of absolute UV coordinates (0.0-1.0)
// to a rectangle defined by (x, y, w, h)
float2 mapUVToRect(float4 rect, float2 uv) {
    //return float2(
    //    rect.x + rect.z * uv.x,
    //    rect.y + rect.w * uv.y
    //);
    return float2(
        rect.z * uv.x,
        rect.w * uv.y
    );
}
