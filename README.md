# `Estimação Estéreo de Pose Corporal 3D`

O presente projeto foi originado no contexto das atividades da disciplina de pós-graduação *IA904 - Projeto de Modelos em Computação Visual*, 
oferecida no primeiro semestre de 2024, na Unicamp, sob supervisão da Profa. Dra. Leticia Rittner e da Profa. Paula D. Paro Costa, ambas do Departamento de Engenharia de Computação e Automação (DCA) da Faculdade de Engenharia Elétrica e de Computação (FEEC).

Nome|RA|Curso
-|-|-
Elton Cardoso do Nascimento|233840|Mestrado em Engenharia Elétrica - Área de Concentração AE: Engenharia de Computação
Leonardo Rener de Oliveira|201270|Mestrado em Engenharia Elétrica - Área de Concentração AE: Engenharia de Computação

## Descrição do Projeto

Estimação de pose corporal é uma técnica amplamente utilizada em diversas indústrias como cinema e jogos, e se baseia na detecção de keypoints específicos no corpo de uma pessoa. Pode ser realizada desde utilizando equipamentos mais complexos e custosos como sistemas de mocap (motion capture), quanto por sensores mais simples como IMU (unidades de medição inercial), ou, utilizando IA (inteligência artificial), usando imagens de câmeras [1][2].

Mais especificamente, dentro do contexto universitário, é comum a necessidade de coletas simples de poses corporais, seja para testar e validar ideias em projetos de pesquisa, seja para realização de projetos de disciplina. Este tipo de demanda se beneficiaria de um método de coleta rápido e com pouca preparação, não necessitando de uma precisão tão grande quanto sistemas de mocap, como coleta a partir de imagens.

O objetivo deste projeto é então estimar a posição corporal no espaço 3D de uma pessoa, utilizando uma câmera estéreo (dipositivo com várias câmeras). Mais especificamente, este projeto utilizará uma "OAK-D", dipositivo desenvolvido para aplicações de visão computacional que possui três câmeras, duas laterais monocromáticas, global shutter e 720p; e uma central colorida, rolling shutter e 4K. A câmera se comunica com o computador através de um cabo USB-C, e também possui integrado processadores para execução de pipelines de processamento visual e IA [3].
