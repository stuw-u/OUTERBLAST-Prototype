namespace Unity.Mathematics {
    public static class mathUtils {

        public static bool raySphereIntersection (float3 origin, float3 dir, float3 spherePos, float sphereRadius, out float intersectLength, out float3 intersectPoint) {
            intersectLength = 0f;
            intersectPoint = float3.zero;

            float3 m = origin - spherePos;
            float b = math.dot(m, dir);
            float c = math.dot(m, m) - sphereRadius * sphereRadius;

            // Exit if r’s origin outside s (c > 0) and r pointing away from s (b > 0) 
            if(c > 0.0f && b > 0.0f)
                return false;
            float discr = b * b - c;

            // A negative discriminant corresponds to ray missing sphere 
            if(discr < 0.0f)
                return false;

            // Ray now found to intersect sphere, compute smallest t value of intersection
            intersectLength = -b - math.sqrt(discr);

            // If t is negative, ray started inside sphere so clamp t to zero 
            if(intersectLength < 0.0f)
                intersectLength = 0.0f;
            intersectPoint = origin + intersectLength * dir;

            return true;
        }

        public static bool rayQuadIntersection (float3 origin, float3 dir, float3 p1, float3 p2, float3 p3, float3 p4) {
            return rayTrisIntersection(origin, dir, p1, p2, p3) || rayTrisIntersection(origin, dir, p2, p4, p3);
        }

        public static bool rayTrisIntersection (float3 origin, float3 dir, float3 p1, float3 p2, float3 p3) {
            float3 e1, e2; // Vectors from p1 to p2/p3 (edges)
            float3 p, q, t;
            float det, invDet, u, v;

            e1 = p2 - p1; //Find vectors for two edges sharing vertex/point p1
            e2 = p3 - p1;
            p = math.cross(dir, e2); // calculating determinant 
            det = math.dot(e1, p); //Calculate determinat

            if(det > -math.EPSILON && det < math.EPSILON) //if determinant is near zero, ray lies in plane of triangle otherwise not
                return false;
            invDet = 1.0f / det;

            t = origin - p1; //calculate distance from p1 to ray origin
            u = math.dot(t, p) * invDet; //Calculate u parameter

            if(u < 0 || u > 1) //Check for ray hit
                return false;

            q = math.cross(t, e1); //Prepare to test v parameter
            v = math.dot(dir, q) * invDet; //Calculate v parameter

            if(v < 0 || u + v > 1) //Check for ray hit
                return false;

            if((math.dot(e2, q) * invDet) > math.EPSILON) //ray does intersect
                return true;

            // No hit at all
            return false;
        }

        // Do not use, broken
        public static quaternion fromToRotationUnsafe (float3 from, float3 to) {
            float3 axis = math.cross(from, to);
            float angle = mathUtils.angle(from, to);
            if(angle >= 179.9196f) {
                var r = math.cross(from, math.right());
                axis = math.cross(r, from);
                if(math.lengthsq(axis) < 0.000001f)
                    axis = math.up();
            } else if(angle <= 0.0804f) {
                //???? help
            }
            return quaternion.AxisAngle(math.normalize(axis), angle);
        }

        public static float3 accelerateVelocity (float3 velocity, float3 direction, float maxSpeed, float acceleration) {
            float3 target = direction * maxSpeed;
            float3 impulse = direction * acceleration;
            float3 v = velocity;

            if(target.x > 0f && v.x < target.x) {
                v.x = math.min(target.x, v.x + impulse.x);
            } else if(target.x < 0f && v.x > target.x) {
                v.x = math.max(target.x, v.x + impulse.x);
            }

            if(target.y > 0f && v.y < target.y) {
                v.y = math.min(target.y, v.y + impulse.y);
            } else if(target.y < 0f && v.y > target.y) {
                v.y = math.max(target.y, v.y + impulse.y);
            }

            if(target.z > 0f && v.z < target.z) {
                v.z = math.min(target.z, v.z + impulse.z);
            } else if(target.z < 0f && v.z > target.z) {
                v.z = math.max(target.z, v.z + impulse.z);
            }

            return v;
        }

        public static float angle (float3 from, float3 to)
            => math.degrees(math.acos(math.clamp(math.dot(math.normalizesafe(from), math.normalizesafe(to)), -1f, 1f)));

        public static byte map01ToByte (float value)
            => (byte)math.floor(math.saturate(value) * 255f);

        public static byte mapMinus11ToByte (float value)
            => (byte)math.floor(math.saturate(value * 0.5f + 0.5f) * 255f);

        public static float mod (float a, float n) {
            return ((a % n) + n) % n;
        }
    }
}