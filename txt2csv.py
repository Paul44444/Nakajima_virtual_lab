# info (paul): for the muc test: convert txt to csv file

import csv

root_path = '/Users/paulrichter/Desktop/DIC_2025_for_travel/DIC_package/' + \
           'exp_normal/time_flow_v/nice_pics/'
txt_path = root_path + 'gom.txt'
csv_path = root_path + 'gom.csv'

with open(txt_path, 'r') as in_file:
    stripped = [line.strip() for line in in_file]
    lines = [line.split("\t") for line in stripped if line]
    with open(csv_path, 'w') as out_file:
        writer = csv.writer(out_file, delimiter='\t')
        writer.writerow(['x', 'y', 'z', 'strain_x', 'strain_y', 'v_x', 'v_y'])
        writer.writerows(lines)

rows = []
with open(csv_path, newline='') as csvfile:
    spamreader = csv.reader(csvfile, delimiter='\t', quotechar='|')
    for row in spamreader:
        rows.append(row)

#import pandas as pd
#df = pd.read_fwf(txt_path)
#df.to_csv(csv_path)