<?php
// Lê o conteúdo do arquivo JSON
$json_data = file_get_contents('C:\xampp\htdocs\Config\cameras.json');

// Decodifica o JSON em um array associativo
$data = json_decode($json_data, true);

// Acessa os dados das câmeras
$cameras = $data['cameras'];

if (count($cameras) > 0) {
    echo '<ul class="list-group mt-3">';
    foreach ($cameras as $index => $camera) {
        echo '<li class="list-group-item">';
        echo '<div id="camera_' . $index . '">';
        echo '<strong>' . $camera["nome"] . '</strong> - RTSP: ' . $camera["rtsp"];
        echo '<div class="float-right">';
        echo '<button class="btn btn-sm btn-primary mr-2" onclick="editCamera(' . $index . ')"><i class="fa far fa-edit"></i></button>';
        echo '<a href="remover.php?id=' . $index . '" class="btn btn-sm btn-danger"><i class="fa fa-trash-o fa"></i></a>';
        echo '</div>';
        echo '</div>';
        echo '<div id="edit_camera_' . $index . '" style="display: none;">';
        echo '<input type="text" id="nome_' . $index . '" value="' . $camera["nome"] . '">';
        echo '<input type="text" id="rtsp_' . $index . '" value="' . $camera["rtsp"] . '">';
        echo '<button class="btn btn-sm btn-success" onclick="saveCamera(' . $index . ')">Salvar</button>';
        echo '</div>';
        echo '</li>';
    }
    echo '</ul>';
} else {
    echo '<p>Nenhuma câmera registrada.</p>';
}
?>

<script src="https://ajax.googleapis.com/ajax/libs/jquery/3.5.1/jquery.min.js"></script>

<script>
    function editCamera(index) {
        document.getElementById('camera_' + index).style.display = 'none';
        document.getElementById('edit_camera_' + index).style.display = 'block';
    }

    function saveCamera(index) {
        var nome = document.getElementById('nome_' + index).value;
        var rtsp = document.getElementById('rtsp_' + index).value;

        $.ajax({
            url: 'editar.php',
            method: 'POST',
            data: { id: index, nome: nome, rtsp: rtsp },
            dataType: 'json',
            success: function(response) {
                if (response.success)
                 {
                    location.reload();
                } 
                else 
                {
                    // Se ocorreu algum erro, você pode lidar com isso aqui, como mostrar uma mensagem de erro
                }
            },
            error: function(xhr, status, error) 
            {

            }
        });
    }
</script>

