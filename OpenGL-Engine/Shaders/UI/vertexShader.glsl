#version 330 core
                layout(location = 0) in vec2 aPosition;
                layout(location = 1) in vec2 aUV;
                layout(location = 2) in vec4 aColor;
                uniform mat4 projection_matrix;
                out vec2 vUV;
                out vec4 vColor;
                void main()
                {
                    vUV = aUV;
                    vColor = aColor;
                    gl_Position = projection_matrix * vec4(aPosition.xy, 0.0, 1.0);
                }