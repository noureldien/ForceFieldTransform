clc;

% read the image
img = imread('Ear 5.jpg');
img = rgb2gray(img);

[img_h, img_w] = size(img);
img_force = zeros(img_h, img_w);

distance = 0;
force = zeros(2,1);
r = zeros(2,1);

offset = 12;
kernel = offset*2 + 1;

for y=offset+1:img_h-offset
    for x=offset+1:img_w-offset
        force = zeros(2,1);
        r = zeros(2,1);
        for yy=y-offset:y+offset
            for xx=x-offset:x+offset
                if (xx ~= x && yy ~= y)
                    r = [xx-x; yy-y];
                    force = force + (r*(double(img(yy,xx))/norm(r)^3));
                end
            end
        end
        img_force(y,x)= norm(force);
    end
end

% display the image (original and force field)
img_force = uint8(img_force);
figure(1); clf;
imshow(img_force);
