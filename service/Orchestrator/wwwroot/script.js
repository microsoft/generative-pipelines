async function fetchTools() {
    try {
        const response = await fetch('/tools');
        if (!response.ok) {
            throw new Error('Failed to fetch tools');
        }
        const data = await response.json();
        const tools = data.tools; // Extract "tools" object
        const functions = data.functions || {}; // Extract "functions" object

        const list = document.getElementById('tools-list');
        list.innerHTML = ''; // Clear existing content

        if (Object.keys(tools).length === 0) {
            list.innerHTML = '<li>No tools available</li>';
            return;
        }

        for (const tool of Object.values(tools)) {
            // Create tool entry
            const li = document.createElement('li');

            const toolLink = document.createElement('a');
            toolLink.href = tool.endpoint;
            toolLink.textContent = tool.name;
            toolLink.classList.add("tool-name");
            toolLink.target = "_blank"; // Open in new tab
            li.appendChild(toolLink);

            // Add Swagger link if available
            if (tool.swaggerUrl) {
                const swaggerLink = document.createElement("a");
                swaggerLink.href = tool.swaggerUrl;
                swaggerLink.textContent = " [swagger]";
                swaggerLink.classList.add("swagger-link");
                swaggerLink.target = "_blank";
                li.appendChild(swaggerLink);
            }

            // Find matching functions for this tool
            const functionList = Object.values(functions).filter(func => func.id.startsWith(tool.name + "/"));

            if (functionList.length > 0) {
                const subUl = document.createElement('ul');
                subUl.classList.add("functions");

                functionList.forEach(func => {
                    const subLi = document.createElement('li');
                    subLi.classList.add("function-item");

                    const nameSpan = document.createElement('span');
                    nameSpan.classList.add("function-name");
                    nameSpan.textContent = `ID: ${func.id}`; // Uses "ID" instead of "Function ID"

                    const descSpan = document.createElement('span');
                    descSpan.classList.add("function-desc");
                    descSpan.textContent = func.description || "";

                    subLi.appendChild(nameSpan);
                    subLi.appendChild(descSpan);
                    subUl.appendChild(subLi);
                });

                li.appendChild(subUl);
            }

            list.appendChild(li);
        }
    } catch (error) {
        console.error(error);
        document.getElementById('tools-list').innerHTML = '<li>Error loading tools</li>';
    }
}

fetchTools();