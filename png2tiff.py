#import time
#
#print("\n Hello world 216")
#
#with open("hello_215.txt", "w") as file:
#    file.write("Your paul text goes here")
#
#print("\n have waited 215 seconds")

from PIL import Image
import time

for i in range(7):
    for experiment in ["exp_normal", "lighting", "speckle"]:
        for side in ["cam_0", "cam_1"]:#01072024 ["left", "right"]:
            #01072024 im_name = "C:/Users/go73jem/Desktop/DIC_package/" + side + "/im_" + str(i) + ".png"
            #01072024 tif_name = "C:/Users/go73jem/Desktop/DIC_package/" + side + "/im_" + str(i).zfill(2) + ".tif"
            #01072024 im_name = "C:/Users/go73jem/Desktop/DIC_package/" + side + "/im_" + str(i) + ".png"
            #01072024 tif_name = "C:/Users/go73jem/Desktop/DIC_package/" + side + "/im_" + str(i).zfill(2) + ".tif"
            im_name = "C:/Users/go73jem/Desktop/DIC_package/" + experiment + "/" + side + "/uv/im_" + str(i) + ".png"
            tif_name = "C:/Users/go73jem/Desktop/DIC_package/" + experiment + "/" + side + "/uv/im_" + str(i).zfill(2) + ".tif"
            img = Image.open(im_name)
            img.save(tif_name)

print("tif success")
#time.sleep(1)