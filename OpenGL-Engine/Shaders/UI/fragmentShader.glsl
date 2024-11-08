 #version 330 core
                in vec2 vUV;
                in vec4 vColor;
                uniform sampler2D Texture;
                out vec4 FragColor;
                void main()
                {
                    FragColor = vColor * texture(Texture, vUV.st);
                }