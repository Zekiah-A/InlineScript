/*3023781b-79b3-4a14-8b50-c3686a7bfdd0*/
let centre = document.getElementById("centre")
let clickeyClickey = document.getElementById("clickeyClickey")
/*c092d4ed-66b6-4951-ad5e-f15301c29660*/
/*7297b55e-1e15-4061-9855-b32212830c2a*/
/*{"GuidSegment":"90eed0a7","Id":"document.getElementById(\u00222c6360b9\u0022)","HandlerName":"onmouseover"}*/
document.getElementById("2c6360b9").onmouseover = function(event) {

                alert('I\'ve been hovered!\nDid you know the button below me has been clicked ' + clicked + ' times?')
                document.getElementById("2c6360b9").style.background = 'blue';
}
/*7afbeafb-55c8-4486-8630-ceec318a7691*/
/*523e8cf1-03e6-4028-bb64-baf5d4f68b6e*/

        let clicked = 0

        clickeyClickey.addEventListener("mousedown", () => {
            clickeyClickey.innerText = "I've been clicked " + clicked + " times"
            clicked++
        })
    /*15705e67-66c9-4cf9-aac1-c284ff037295*/
