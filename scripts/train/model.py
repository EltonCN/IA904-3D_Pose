import torch
import numpy as np

import torch
import numpy as np

class ConvBlock(torch.nn.Module):
    def __init__(self, input_img_size, input_channels, output_channels, dropout_rate) -> None:
        super().__init__()

        self._conv = torch.nn.Conv2d(input_channels, output_channels, kernel_size=(3,3), stride=1, padding=1)
        self._dropout = torch.nn.Dropout(dropout_rate)
        self._relu = torch.nn.ReLU()
        #self._max_pool = torch.nn.MaxPool2d(kernel_size=(2,2))
        self._batch_norm = torch.nn.BatchNorm2d(output_channels)
        
        self._conv1 = torch.nn.Conv2d(input_channels, output_channels, kernel_size=1)

        self._output_channels = output_channels
        self._output_img_size = np.array(input_img_size)
        
        #Conv
        self._output_img_size[0] = ((self._output_img_size[0] + (2*1) - 1* (3-1)-1)/1)+1
        self._output_img_size[1] = ((self._output_img_size[1] + (2*1) - 1* (3-1)-1)/1)+1

        #Max pool
        #self._output_img_size[0] = ((self._output_img_size[0] + (2*0) - 1* (2-1)-1)/2)+1
        #self._output_img_size[1] = ((self._output_img_size[1] + (2*0) - 1* (2-1)-1)/2)+1

    @property
    def output_img_size(self) -> np.ndarray:
        return self._output_img_size.copy()
    
    @property
    def output_channels(self) -> int:
        return self._output_channels
    

    def forward(self, x):
        y1 = self._conv(x)
        y1 = self._dropout(y1)
        y1 = self._relu(y1)
        #y1 = self._max_pool(y1)

        y2 = self._conv1(x)

        y = y1+y2

        y = self._batch_norm(y)

        return y


class OAKD3DKeypoint(torch.nn.Module):
    def __init__(self, img_size, convolutional_sizes, dense_hidden_size, dropout_rate=0.1):
        super().__init__()

        convolutional_blocks = []
        input_img_size = img_size
        input_channels = 3
        for i in range(len(convolutional_sizes)):
            block = ConvBlock(input_img_size, input_channels, convolutional_sizes[i], dropout_rate)
            convolutional_blocks.append(block)

            input_img_size = block.output_img_size
            input_channels = block.output_channels
            
        self._conv = torch.nn.Sequential(*convolutional_blocks)

        n_features = np.prod(input_img_size)*input_channels
        
        self._flatten = torch.nn.Flatten()
        self._linear1 = torch.nn.Linear(n_features,dense_hidden_size)
        self._dropout1 = torch.nn.Dropout(dropout_rate)
        self._relu = torch.nn.ReLU()
        self._linear2 = torch.nn.Linear(dense_hidden_size, 3)

    def forward(self, x):
        y = self._conv(x)
        y = self._flatten(y)
        y = self._dropout1(self._linear1(y))
        y = self._relu(y)
        y = self._linear2(y)

        return y