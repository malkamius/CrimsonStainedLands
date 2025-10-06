var F=class{constructor(e){this.aliasesList=null;this.editFormContainer=null;this.addAliasBtn=null;this.app=e,this.initializeUIElements(),this.initializeEventListeners()}initializeUIElements(){this.aliasesList=document.getElementById("aliases-list"),this.editFormContainer=document.getElementById("alias-edit-form-container"),this.addAliasBtn=document.getElementById("add-alias")}initializeEventListeners(){this.addAliasBtn&&this.addAliasBtn.addEventListener("click",()=>{this.showEditForm(null,-1)})}loadAliases(){!this.aliasesList||!this.app.settings.Aliases||(this.aliasesList.innerHTML="",this.editFormContainer&&(this.editFormContainer.innerHTML="",this.editFormContainer.classList.remove("active")),this.app.settings.Aliases.forEach((e,i)=>{if(this.aliasesList===null)return;let t=document.createElement("tr");t.dataset.index=i.toString();let l=document.createElement("td");l.textContent=e.alias,t.appendChild(l);let a=document.createElement("td");a.textContent=e.command,t.appendChild(a);let n=document.createElement("td"),s=document.createElement("button");s.className="client-button",s.textContent="Edit",s.style.marginRight="5px",s.addEventListener("click",()=>{this.showEditForm(e,i)});let o=document.createElement("button");o.className="client-button",o.textContent="Delete",o.addEventListener("click",()=>{this.app.settings.Aliases.splice(i,1),this.app.saveSettings(),this.loadAliases()}),n.appendChild(s),n.appendChild(o),t.appendChild(n),this.aliasesList.appendChild(t)}))}showEditForm(e,i){if(!this.editFormContainer)return;this.editFormContainer.innerHTML="";let t=document.createElement("h4");t.textContent=i===-1?"Add New Alias":"Edit Alias",this.editFormContainer.appendChild(t);let l=document.createElement("div");l.className="form-row";let a=document.createElement("label");a.textContent="Alias:",a.setAttribute("for","edit-alias-input");let n=document.createElement("input");n.type="text",n.id="edit-alias-input",n.value=e?e.alias:"",n.placeholder="e.g., n, sw, l",l.appendChild(a),l.appendChild(n),this.editFormContainer.appendChild(l);let s=document.createElement("div");s.className="form-row";let o=document.createElement("label");o.textContent="Command:",o.setAttribute("for","edit-alias-cmd-input");let r=document.createElement("input");r.type="text",r.id="edit-alias-cmd-input",r.value=e?e.command:"",r.placeholder="e.g., north, southwest, look",s.appendChild(o),s.appendChild(r),this.editFormContainer.appendChild(s);let d=document.createElement("div");d.innerHTML='<small>Tip: Aliases let you type a shorter command that expands to a longer one. For example, use "n" for "north".</small>',d.style.marginTop="5px",d.style.color="#999",this.editFormContainer.appendChild(d);let p=document.createElement("div");p.className="button-row";let h=document.createElement("button");h.className="client-button",h.textContent="Save",h.addEventListener("click",()=>{i===-1?this.app.settings.Aliases.push({alias:n.value,command:r.value}):e&&(e.alias=n.value,e.command=r.value),this.app.saveSettings(),this.loadAliases()});let c=document.createElement("button");c.className="client-button",c.textContent="Cancel",c.style.backgroundColor="#555",c.addEventListener("click",()=>{this.editFormContainer.innerHTML="",this.editFormContainer.classList.remove("active")}),p.appendChild(h),p.appendChild(c),this.editFormContainer.appendChild(p),this.editFormContainer.classList.add("active")}updateUI(){this.loadAliases()}};var P=class{constructor(e){this.triggersList=null;this.editFormContainer=null;this.addTriggerBtn=null;this.app=e,this.initializeUIElements(),this.initializeEventListeners()}initializeUIElements(){this.triggersList=document.getElementById("triggers-list"),this.editFormContainer=document.getElementById("trigger-edit-form-container"),this.addTriggerBtn=document.getElementById("add-trigger")}initializeEventListeners(){this.addTriggerBtn&&this.addTriggerBtn.addEventListener("click",()=>{this.showEditForm(null,-1)})}loadTriggers(){!this.triggersList||!this.app.settings.Triggers||(this.triggersList.innerHTML="",this.editFormContainer&&(this.editFormContainer.innerHTML="",this.editFormContainer.classList.remove("active")),this.app.settings.Triggers.forEach((e,i)=>{if(this.triggersList===null)return;let t=document.createElement("tr");t.dataset.index=i.toString();let l=document.createElement("td");l.textContent=e.match,t.appendChild(l);let a=document.createElement("td");a.textContent=e.type||"regex",t.appendChild(a);let n=document.createElement("td");n.textContent=e.actionType||"text",t.appendChild(n);let s=document.createElement("td");s.textContent=e.actions,t.appendChild(s);let o=document.createElement("td"),r=document.createElement("button");r.className="client-button",r.textContent="Edit",r.style.marginRight="5px",r.addEventListener("click",()=>{this.showEditForm(e,i)});let d=document.createElement("button");d.className="client-button",d.textContent="Delete",d.addEventListener("click",()=>{this.app.settings.Triggers.splice(i,1),this.app.saveSettings(),this.loadTriggers()}),o.appendChild(r),o.appendChild(d),t.appendChild(o),this.triggersList.appendChild(t)}))}showEditForm(e,i){if(!this.editFormContainer)return;this.editFormContainer.innerHTML="";let t=document.createElement("h4");t.textContent=i===-1?"Add New Trigger":"Edit Trigger",this.editFormContainer.appendChild(t);let l=document.createElement("div");l.className="form-row";let a=document.createElement("label");a.textContent="Pattern:",a.setAttribute("for","edit-trigger-pattern");let n=document.createElement("input");n.type="text",n.id="edit-trigger-pattern",n.value=e?e.match:"",n.placeholder="e.g., ^You are hungry\\.$",l.appendChild(a),l.appendChild(n),this.editFormContainer.appendChild(l);let s=document.createElement("div");s.className="form-row";let o=document.createElement("label");o.textContent="Match Type:",o.setAttribute("for","edit-trigger-type");let r=document.createElement("select");r.id="edit-trigger-type",["regex","substring","exact"].forEach(v=>{let g=document.createElement("option");g.value=v,g.textContent=v.charAt(0).toUpperCase()+v.slice(1),(e&&e.type===v||!e&&v==="regex")&&(g.selected=!0),r.appendChild(g)}),s.appendChild(o),s.appendChild(r),this.editFormContainer.appendChild(s);let p=document.createElement("div");p.className="form-row";let h=document.createElement("label");h.textContent="Action Type:",h.setAttribute("for","edit-trigger-action-type");let c=document.createElement("select");c.id="edit-trigger-action-type",["text","javascript"].forEach(v=>{let g=document.createElement("option");g.value=v,g.textContent=v.charAt(0).toUpperCase()+v.slice(1),(e&&e.actionType===v||!e&&v==="text")&&(g.selected=!0),c.appendChild(g)}),p.appendChild(h),p.appendChild(c),this.editFormContainer.appendChild(p);let E=document.createElement("div");E.className="form-row";let L=document.createElement("label");L.textContent="Actions:",L.setAttribute("for","edit-trigger-actions");let f=document.createElement("textarea");f.id="edit-trigger-actions",f.value=e?e.actions:"",f.placeholder="Enter commands or JavaScript code",c.addEventListener("change",()=>{c.value==="text"?f.placeholder="Enter commands to execute when triggered":f.placeholder="Enter JavaScript code to execute when triggered"}),E.appendChild(L),E.appendChild(f),this.editFormContainer.appendChild(E);let m=document.createElement("div");m.innerHTML="<small>Tip: Triggers automatically execute actions when matching text appears in the MUD output.<br>Text actions are sent to the MUD as commands.<br>JavaScript actions are executed in the browser and can use <code>window.mudApp</code> to interact with the client.</small>",m.style.marginTop="5px",m.style.color="#999",this.editFormContainer.appendChild(m);let u=document.createElement("div");u.className="pattern-test-container";let b=document.createElement("h4");b.textContent="Test Pattern",u.appendChild(b);let x=document.createElement("div");x.className="form-row";let I=document.createElement("label");I.textContent="Test Text:",I.setAttribute("for","pattern-test-input");let T=document.createElement("textarea");T.id="pattern-test-input",T.placeholder="Enter text to test against the pattern",T.rows=4,T.style.width="calc(100% - 110px)",T.style.maxWidth="400px",x.appendChild(I),x.appendChild(T),u.appendChild(x);let S=document.createElement("button");S.className="client-button",S.textContent="Test Pattern",S.addEventListener("click",()=>{let v=n.value,g=r.value,V=T.value;if(!v||!V)return;let z=!1;try{z=this.app.matchTrigger(V,g,v);let C=document.getElementById("pattern-test-result");C||(C=document.createElement("div"),C.id="pattern-test-result",C.className="pattern-test-result",u.appendChild(C)),z?(C.textContent="Match found! The trigger would activate.",C.className="pattern-test-result success"):(C.textContent="No match. The trigger would not activate.",C.className="pattern-test-result failure")}catch(C){let w=document.getElementById("pattern-test-result");w||(w=document.createElement("div"),w.id="pattern-test-result",w.className="pattern-test-result",u.appendChild(w)),w.textContent=`Error: ${C instanceof Error?C.message:"Unknown error"}`,w.className="pattern-test-result failure"}}),u.appendChild(S),this.editFormContainer.appendChild(u);let B=document.createElement("div");B.className="button-row",B.style.marginTop="20px";let A=document.createElement("button");A.className="client-button",A.textContent="Save",A.addEventListener("click",()=>{i===-1?this.app.settings.Triggers.push({match:n.value,type:r.value,actions:f.value,actionType:c.value}):e&&(e.match=n.value,e.type=r.value,e.actions=f.value,e.actionType=c.value),this.app.saveSettings(),this.loadTriggers()});let M=document.createElement("button");M.className="client-button",M.textContent="Cancel",M.style.backgroundColor="#555",M.addEventListener("click",()=>{this.editFormContainer&&(this.editFormContainer.innerHTML="",this.editFormContainer.classList.remove("active"))}),B.appendChild(A),B.appendChild(M),this.editFormContainer.appendChild(B),this.editFormContainer.classList.add("active"),e&&e.actionType==="javascript"&&(f.placeholder="Enter JavaScript code to execute when triggered")}updateUI(){this.loadTriggers()}};var H=class{constructor(e){this.keysList=null;this.editFormContainer=null;this.addKeyBtn=null;this.app=e,this.initializeUIElements(),this.initializeEventListeners()}initializeUIElements(){this.keysList=document.getElementById("keys-list"),this.editFormContainer=document.getElementById("edit-form-container"),this.addKeyBtn=document.getElementById("add-key")}initializeEventListeners(){this.addKeyBtn&&this.addKeyBtn.addEventListener("click",()=>{this.showEditForm(null,-1)})}loadKeyBindings(){!this.keysList||!this.app.settings.Keybindings||(this.keysList.innerHTML="",this.editFormContainer&&(this.editFormContainer.innerHTML="",this.editFormContainer.classList.remove("active")),this.app.settings.Keybindings.forEach((e,i)=>{if(this.keysList===null)return;let t=document.createElement("tr");t.dataset.index=i.toString();let l=document.createElement("td");l.textContent=e.key,t.appendChild(l);let a=document.createElement("td");a.textContent=e.commands,t.appendChild(a);let n=document.createElement("td"),s=document.createElement("button");s.className="client-button",s.textContent="Edit",s.style.marginRight="5px",s.addEventListener("click",()=>{this.showEditForm(e,i)});let o=document.createElement("button");o.className="client-button",o.textContent="Delete",o.addEventListener("click",()=>{this.app.settings.Keybindings.splice(i,1),this.app.saveSettings(),this.loadKeyBindings()}),n.appendChild(s),n.appendChild(o),t.appendChild(n),this.keysList.appendChild(t)}))}showEditForm(e,i){if(!this.editFormContainer)return;this.editFormContainer.innerHTML="";let t=document.createElement("h4");t.textContent=i===-1?"Add New Key Binding":"Edit Key Binding",this.editFormContainer.appendChild(t);let l=document.createElement("div");l.className="form-row";let a=document.createElement("label");a.textContent="Key:",a.setAttribute("for","edit-key-input");let n=document.createElement("input");n.type="text",n.id="edit-key-input",n.value=e?e.key:"",n.placeholder="e.g., Numpad8, KeyA, ArrowUp";let s=document.createElement("button");s.className="client-button",s.textContent="Capture Key",s.className="capture-key-btn",s.type="button",l.appendChild(a),l.appendChild(n),l.appendChild(s),this.editFormContainer.appendChild(l);let o=document.createElement("div");o.className="form-row";let r=document.createElement("label");r.textContent="Command:",r.setAttribute("for","edit-cmd-input");let d=document.createElement("input");d.type="text",d.id="edit-cmd-input",d.value=e?e.commands:"",d.placeholder="e.g., north, look, /disconnect",o.appendChild(r),o.appendChild(d),this.editFormContainer.appendChild(o);let p=document.createElement("div");p.innerHTML='<small>Tip: Press "Capture Key" and press any key combination to automatically set the key.</small>',p.style.marginTop="5px",p.style.color="#999",this.editFormContainer.appendChild(p);let h=document.createElement("div");h.className="button-row";let c=document.createElement("button");c.className="client-button",c.textContent="Save",c.addEventListener("click",()=>{i===-1?this.app.settings.Keybindings.push({key:n.value,commands:d.value}):e&&(e.key=n.value,e.commands=d.value),this.app.saveSettings(),this.loadKeyBindings()});let y=document.createElement("button");y.className="client-button",y.textContent="Cancel",y.style.backgroundColor="#555",y.addEventListener("click",()=>{this.editFormContainer&&(this.editFormContainer.innerHTML="",this.editFormContainer.classList.remove("active"))}),h.appendChild(c),h.appendChild(y),this.editFormContainer.appendChild(h),this.editFormContainer.classList.add("active"),s.addEventListener("click",()=>{this.showKeyCaptureDialog(n)})}showKeyCaptureDialog(e){let i=document.createElement("div");i.className="key-capture-overlay";let t=document.createElement("div");t.className="key-capture-dialog";let l=document.createElement("div");l.className="key-capture-content";let a=document.createElement("h3");a.textContent="Waiting for key...";let n=document.createElement("p");n.textContent="Press any key or key combination to capture it.";let s=document.createElement("div");s.className="key-display",s.textContent="Press a key";let o=document.createElement("button");o.className="client-button",o.textContent="Cancel",o.addEventListener("click",()=>{document.body.removeChild(i),document.removeEventListener("keydown",r)}),l.appendChild(a),l.appendChild(n),l.appendChild(s),l.appendChild(o),t.appendChild(l),i.appendChild(t),document.body.appendChild(i),t.focus();let r=d=>{if(d.preventDefault(),d.stopPropagation(),d.key==="Control"||d.key==="Alt"||d.key==="Shift"||d.key==="Meta"||d.code==="ControlLeft"||d.code==="ControlRight"||d.code==="AltLeft"||d.code==="AltRight"||d.code==="ShiftLeft"||d.code==="ShiftRight"){s.textContent="Waiting for a non-modifier key...";return}let p="";d.ctrlKey&&(p+="Ctrl+"),d.altKey&&(p+="Alt+"),d.shiftKey&&(p+="Shift+"),p+=d.code,s.textContent=p,e.value=p,setTimeout(()=>{document.body.removeChild(i),document.removeEventListener("keydown",r)},500)};document.addEventListener("keydown",r)}updateUI(){this.loadKeyBindings()}};var N=class{constructor(e){this.variablesList=null;this.editFormContainer=null;this.addVariableBtn=null;this.app=e,this.initializeUIElements(),this.initializeEventListeners()}initializeUIElements(){this.variablesList=document.getElementById("variables-list"),this.editFormContainer=document.getElementById("variable-edit-form-container"),this.addVariableBtn=document.getElementById("add-variable")}initializeEventListeners(){this.addVariableBtn&&this.addVariableBtn.addEventListener("click",()=>{this.showEditForm(null,-1)})}loadVariables(){!this.variablesList||!this.app.settings.Variables||(this.variablesList.innerHTML="",this.editFormContainer&&(this.editFormContainer.innerHTML="",this.editFormContainer.classList.remove("active")),this.app.settings.Variables.forEach((e,i)=>{if(this.variablesList===null)return;let t=document.createElement("tr");t.dataset.index=i.toString();let l=document.createElement("td");l.textContent=e.name,t.appendChild(l);let a=document.createElement("td");a.textContent=e.type||"string",t.appendChild(a);let n=document.createElement("td");n.textContent=e.value,t.appendChild(n);let s=document.createElement("td"),o=document.createElement("button");o.className="client-button",o.textContent="Edit",o.style.marginRight="5px",o.addEventListener("click",()=>{this.showEditForm(e,i)});let r=document.createElement("button");r.className="client-button",r.textContent="Delete",r.addEventListener("click",()=>{this.app.settings.Variables.splice(i,1),this.app.saveSettings(),this.loadVariables()}),s.appendChild(o),s.appendChild(r),t.appendChild(s),this.variablesList.appendChild(t)}))}showEditForm(e,i){if(!this.editFormContainer)return;this.editFormContainer.innerHTML="";let t=document.createElement("h4");t.textContent=i===-1?"Add New Variable":"Edit Variable",this.editFormContainer.appendChild(t);let l=document.createElement("div");l.className="form-row";let a=document.createElement("label");a.textContent="Name:",a.setAttribute("for","edit-variable-name");let n=document.createElement("input");n.type="text",n.id="edit-variable-name",n.value=e?e.name:"",n.placeholder="e.g., HP, TARGET, LOCATION",l.appendChild(a),l.appendChild(n),this.editFormContainer.appendChild(l);let s=document.createElement("div");s.className="form-row";let o=document.createElement("label");o.textContent="Type:",o.setAttribute("for","edit-variable-type");let r=document.createElement("select");r.id="edit-variable-type",["string","number","boolean"].forEach(m=>{let u=document.createElement("option");u.value=m,u.textContent=m.charAt(0).toUpperCase()+m.slice(1),(e&&e.type===m||!e&&m==="string")&&(u.selected=!0),r.appendChild(u)}),s.appendChild(o),s.appendChild(r),this.editFormContainer.appendChild(s);let p=document.createElement("div");p.className="form-row";let h=document.createElement("label");h.textContent="Value:",h.setAttribute("for","edit-variable-value");let c=document.createElement("input");c.type="text",c.id="edit-variable-value",c.value=e?e.value:"",c.placeholder="Enter variable value",p.appendChild(h),p.appendChild(c),this.editFormContainer.appendChild(p);let y=document.createElement("div");y.innerHTML='<small>Tip: Variables can be used in commands with the $VARNAME syntax. For example, "attack $TARGET" will be replaced with the value of the TARGET variable.</small>',y.style.marginTop="5px",y.style.color="#999",this.editFormContainer.appendChild(y);let E=document.createElement("div");E.className="button-row",E.style.marginTop="20px";let L=document.createElement("button");L.className="client-button",L.textContent="Save",L.addEventListener("click",()=>{let m=n.value.trim().toUpperCase();if(!m){this.app.showNotification("Variable name cannot be empty",!1);return}if(!/^[A-Z0-9_]+$/.test(m)){this.app.showNotification("Variable name can only contain letters, numbers, and underscores",!1);return}if(this.app.settings.Variables.findIndex(I=>I.name.toUpperCase()===m&&(i===-1||this.app.settings.Variables.indexOf(I)!==i))!==-1){this.app.showNotification(`A variable with the name "${m}" already exists`,!1);return}let b=c.value,x=r.value;if(x==="number"){let I=parseFloat(b);if(isNaN(I)){this.app.showNotification("Please enter a valid number",!1);return}b=I.toString()}else x==="boolean"&&(b=b.toLowerCase(),["true","1","yes","y","on"].includes(b)?b="true":b="false");i===-1?this.app.settings.Variables.push({name:m,type:x,value:b}):e&&(e.name=m,e.type=x,e.value=b),this.app.saveSettings(),this.loadVariables()});let f=document.createElement("button");if(f.className="client-button",f.textContent="Cancel",f.style.backgroundColor="#555",f.addEventListener("click",()=>{this.editFormContainer&&(this.editFormContainer.innerHTML="",this.editFormContainer.classList.remove("active"))}),E.appendChild(L),E.appendChild(f),this.editFormContainer.appendChild(E),this.editFormContainer.classList.add("active"),r.addEventListener("change",()=>{let m=r.value;if(m==="boolean"){c.placeholder="Enter true or false";let u=c.value.toLowerCase();["true","false","0","1","yes","no","y","n","on","off"].includes(u)||(c.value="false")}else if(m==="number"){c.placeholder="Enter a number",c.type="number";let u=parseFloat(c.value);isNaN(u)&&(c.value="0")}else c.placeholder="Enter variable value",c.type="text"}),e&&e.type){let m=new Event("change");r.dispatchEvent(m)}}updateUI(){this.loadVariables()}};var U=class{constructor(e){this.profileSelect=null;this.deleteProfileSelect=null;this.quickProfileSelect=null;this.newProfileNameInput=null;this.switchProfileBtn=null;this.createProfileBtn=null;this.deleteProfileBtn=null;this.profileConfirmModal=null;this.profileConfirmClose=null;this.confirmDeleteProfileBtn=null;this.cancelDeleteProfileBtn=null;this.profileToDelete="";this.app=e,this.initializeUIElements(),this.initializeEventListeners()}initializeUIElements(){this.profileSelect=document.getElementById("profile-select"),this.deleteProfileSelect=document.getElementById("delete-profile-select"),this.quickProfileSelect=document.getElementById("quick-profile-select"),this.newProfileNameInput=document.getElementById("new-profile-name"),this.switchProfileBtn=document.getElementById("switch-profile"),this.createProfileBtn=document.getElementById("create-profile"),this.deleteProfileBtn=document.getElementById("delete-profile"),this.profileConfirmModal=document.getElementById("profile-confirm-modal"),this.profileConfirmClose=document.getElementById("profile-confirm-close"),this.confirmDeleteProfileBtn=document.getElementById("confirm-delete-profile"),this.cancelDeleteProfileBtn=document.getElementById("cancel-delete-profile")}initializeEventListeners(){this.switchProfileBtn&&this.profileSelect&&this.switchProfileBtn.addEventListener("click",()=>{let e=this.profileSelect.value;e&&this.app.switchProfile(e)&&(this.app.showNotification(`Profile switched to "${e}"`,!0),this.updateUI())}),this.createProfileBtn&&this.newProfileNameInput&&this.createProfileBtn.addEventListener("click",()=>{let e=this.newProfileNameInput.value.trim();e?this.app.createProfile(e)?(this.app.showNotification(`Profile "${e}" created and activated`,!0),this.newProfileNameInput.value="",this.updateUI()):this.app.showNotification(`A profile named "${e}" already exists`,!1):this.app.showNotification("Please enter a profile name",!1)}),this.deleteProfileBtn&&this.deleteProfileSelect&&this.profileConfirmModal&&this.deleteProfileBtn.addEventListener("click",()=>{this.deleteProfileSelect.value&&(this.profileToDelete=this.deleteProfileSelect.value,this.profileConfirmModal.style.display="block",this.app.setModalOpen(!0))}),this.profileConfirmClose&&this.profileConfirmModal&&this.profileConfirmClose.addEventListener("click",()=>{this.profileConfirmModal.style.display="none",this.profileToDelete="",this.app.setModalOpen(!1)}),this.cancelDeleteProfileBtn&&this.profileConfirmModal&&this.cancelDeleteProfileBtn.addEventListener("click",()=>{this.profileConfirmModal.style.display="none",this.profileToDelete="",this.app.setModalOpen(!1)}),this.confirmDeleteProfileBtn&&this.profileConfirmModal&&this.confirmDeleteProfileBtn.addEventListener("click",()=>{this.profileToDelete&&(this.app.deleteProfile(this.profileToDelete)?this.app.showNotification(`Profile "${this.profileToDelete}" deleted`,!0):this.app.showNotification("Cannot delete the Default profile",!1)),this.profileConfirmModal.style.display="none",this.profileToDelete="",this.app.setModalOpen(!1),this.updateUI()}),this.profileConfirmModal&&this.profileConfirmModal.addEventListener("click",e=>{if(e.target===this.profileConfirmModal){if(this.profileConfirmModal===null)return;this.profileConfirmModal.style.display="none",this.profileToDelete="",this.app.setModalOpen(!1)}}),this.quickProfileSelect&&(this.quickProfileSelect.addEventListener("focus",()=>{this.app.setInteractingWithDropdown(!0)}),this.quickProfileSelect.addEventListener("blur",()=>{this.app.setInteractingWithDropdown(!1)}),this.quickProfileSelect.addEventListener("change",()=>{let e=this.quickProfileSelect.value;if(e&&this.app.switchProfile(e)){this.app.showNotification(`Profile switched to "${e}"`,!0),this.updateUI();let i=document.getElementById("input");i&&i.select()}this.app.setInteractingWithDropdown(!1)})),this.profileSelect&&(this.profileSelect.addEventListener("focus",()=>{this.app.setInteractingWithDropdown(!0)}),this.profileSelect.addEventListener("blur",()=>{this.app.setInteractingWithDropdown(!1)})),this.deleteProfileSelect&&(this.deleteProfileSelect.addEventListener("focus",()=>{this.app.setInteractingWithDropdown(!0)}),this.deleteProfileSelect.addEventListener("blur",()=>{this.app.setInteractingWithDropdown(!1)}))}updateUI(){this.populateProfileDropdowns(),this.updateQuickProfileDropdown()}populateProfileDropdowns(){if(!this.profileSelect||!this.deleteProfileSelect)return;let e=this.app.getProfileNames(),i=this.app.getCurrentProfileName();this.profileSelect.innerHTML="",this.deleteProfileSelect.innerHTML="",e.forEach(t=>{if(this.profileSelect===null)return;let l=document.createElement("option");l.value=t,l.textContent=t,t===i&&(l.selected=!0),this.profileSelect.appendChild(l)}),e.forEach(t=>{if(t!=="Default"){let l=document.createElement("option");l.value=t,l.textContent=t,this.deleteProfileSelect.appendChild(l)}}),e.length<=1&&this.deleteProfileBtn&&this.deleteProfileSelect?(this.deleteProfileBtn.disabled=!0,this.deleteProfileSelect.disabled=!0):this.deleteProfileBtn&&this.deleteProfileSelect&&(this.deleteProfileBtn.disabled=!1,this.deleteProfileSelect.disabled=!1)}updateQuickProfileDropdown(){if(!this.quickProfileSelect)return;this.quickProfileSelect.innerHTML="";let e=this.app.getProfileNames(),i=this.app.getCurrentProfileName();e.forEach(t=>{let l=document.createElement("option");l.value=t,l.textContent=t,t===i&&(l.selected=!0),this.quickProfileSelect.appendChild(l)})}};var D=class{constructor(e){this.settingsModal=null;this.modalTitle=null;this.closeBtn=null;this.cancelBtn=null;this.saveBtn=null;this.menuSettings=null;this.menuAliases=null;this.menuTriggers=null;this.menuKeys=null;this.menuVariables=null;this.menuHelp=null;this.sidebarItems=null;this.tabContents=null;this.fontSizeInput=null;this.bgColorInput=null;this.textColorInput=null;this.bgColorValue=null;this.textColorValue=null;this.resetBtn=null;this.app=e,this.createToolbar(),this.createSettingsModal(),this.createImportConfirmModal(),this.createProfileConfirmModal(),this.aliasesUI=new F(e),this.triggersUI=new P(e),this.keyBindingsUI=new H(e),this.variablesUI=new N(e),this.profilesUI=new U(e),this.initializeUIElements(),this.initializeEventListeners(),this.initializeStyles()}createToolbar(){let e=document.createElement("div");e.className="client-menu-bar",e.innerHTML=`
            <ul style='padding-left: 0;	list-style-type: none;'>
            <li class='client-menu-option'>
            <button class="client-button" id="connect-button">Connect</button>
            </li>
            <li class='client-menu-option'>
            <button class="client-button" id="disconnect-button">Disconnect</button>
            </li>
            <li class='client-menu-option'>
            <button class="client-button" id="menu-settings">Settings</button>
            </li>
            <li class='client-menu-option'>
            <button class="client-button" id="menu-aliases">Aliases</button>
            </li>
            <li class='client-menu-option'>
            <button class="client-button" id="menu-triggers">Triggers</button>
            </li>
            <li class='client-menu-option'>
            <button class="client-button" id="menu-keys">Keys</button>
            </li>
            <li class='client-menu-option'>
            <button class="client-button" id="menu-variables">Variables</button>
            </li>
            <li class='client-menu-option'>
            <button class="client-button" id="menu-help">Help</button>
            </li>
            <li class='client-menu-option'>
            <div class="profile-dropdown-container">
                <select id="quick-profile-select" class="profile-dropdown">
                    <!-- Profiles will be populated here via JavaScript -->
                </select>
            </div>
            </li>
            </ul>
        `,new ResizeObserver(t=>{this.app.resize()}).observe(e),this.app.terminalElement&&this.app.terminalElement.parentNode&&this.app.terminalElement.parentNode.insertBefore(e,this.app.terminalElement),this.app.resize()}createSettingsModal(){let e=document.createElement("div");e.className="modal-overlay",e.id="settings-modal",e.innerHTML=`
            <div class="modal-container">
                <div class="modal-sidebar">
                    <div class="sidebar-item active" data-tab="settings">Settings</div>
                    <div class="sidebar-item" data-tab="aliases">Aliases</div>
                    <div class="sidebar-item" data-tab="triggers">Triggers</div>
                    <div class="sidebar-item" data-tab="keys">Keys</div>
                    <div class="sidebar-item" data-tab="variables">Variables</div>
                    <div class="sidebar-item" data-tab="help">Help</div>
                </div>
                <div class="modal-content">
                    <div class="modal-header">
                        <h2 id="modal-title">Settings</h2>
                        <span class="modal-close">&times;</span>
                    </div>
                    
                    <!-- Settings Tab -->
                    <div class="tab-content active" id="settings-tab">
                        <div style="margin-top: 20px; display: flex; gap: 10px;">
                            <button id="export-settings">Export Settings</button>
                            <button id="import-settings">Import Settings</button>
                        </div>
                        <h3>Settings Profiles</h3>
                        <div class="profile-container" style="margin-bottom: 15px; background-color: #222; padding: 15px; border-radius: 4px; border: 1px solid #444;">
                            <div class="form-row" style="margin-bottom: 15px; display: flex; align-items: center;">
                                <label for="profile-select" style="min-width: 120px; color: white;">Current Profile:</label>
                                <select id="profile-select" style="flex-grow: 1; padding: 8px; background-color: #333; color: white; border: 1px solid #444; border-radius: 4px; margin-right: 10px;">
                                    <!-- Profile options will be populated here -->
                                </select>
                                <button id="switch-profile" style="background-color: #8c1f08;">Switch</button>
                            </div>
                            
                            <div class="form-row" style="margin-bottom: 15px; display: flex; align-items: center;">
                                <label for="new-profile-name" style="min-width: 120px; color: white;">New Profile:</label>
                                <input type="text" id="new-profile-name" style="flex-grow: 1; padding: 8px; background-color: #333; color: white; border: 1px solid #444; border-radius: 4px; margin-right: 10px;" placeholder="Enter profile name">
                                <button id="create-profile" style="background-color: #8c1f08;">Create</button>
                            </div>
                            
                            <div class="form-row" style="display: flex; align-items: center;">
                                <label for="delete-profile-select" style="min-width: 120px; color: white;">Delete Profile:</label>
                                <select id="delete-profile-select" style="flex-grow: 1; padding: 8px; background-color: #333; color: white; border: 1px solid #444; border-radius: 4px; margin-right: 10px;">
                                    <!-- Profile options will be populated here, excluding Default -->
                                </select>
                                <button id="delete-profile" style="background-color: #8c1f08;">Delete</button>
                            </div>
                        </div>
                        <h3>General Settings</h3>
                        <div style="margin-bottom: 15px;">
                            <label for="font-size">Font Size:</label>
                            <input type="number" id="font-size" min="8" max="24" value="14">
                        </div>
                        <div style="margin-bottom: 15px;">
                            <label for="bg-color">Background Color:</label>
                            <input type="color" id="bg-color" value="#000000">
                            <span id="bg-color-value">#000000</span>
                        </div>
                        <div style="margin-bottom: 15px;">
                            <label for="text-color">Text Color:</label>
                            <input type="color" id="text-color" value="#FFFFFF">
                            <span id="text-color-value">#FFFFFF</span>
                        </div>
                        <div style="margin-top: 20px;">
                            <button id="reset-settings">Reset to Default</button>
                        </div>
                    </div>
                    
                    <!-- Aliases Tab -->
                    <div class="tab-content" id="aliases-tab">
                        <h3>Aliases</h3>
                        <div>
                            <div class="table-container">
                                <table style="width: 100%;">
                                    <thead>
                                        <tr>
                                            <th>Alias</th>
                                            <th>Command(s)</th>
                                            <th>Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody id="aliases-list">
                                        <!-- Aliases will be added here -->
                                    </tbody>
                                </table>
                            </div>
                            <div id="alias-edit-form-container" class="edit-form-container">
                                <!-- Edit form will be placed here -->
                            </div>
                            <button id="add-alias" class="add-btn">Add Alias</button>
                        </div>
                    </div>
                    
                    <!-- Triggers Tab -->
                    <div class="tab-content" id="triggers-tab">
                        <h3>Triggers</h3>
                        <div>
                            <div class="table-container">
                                <table style="width: 100%;">
                                    <thead>
                                        <tr>
                                            <th>Pattern</th>
                                            <th>Type</th>
                                            <th>Action Type</th>
                                            <th>Actions</th>
                                            <th>Controls</th>
                                        </tr>
                                    </thead>
                                    <tbody id="triggers-list">
                                        <!-- Triggers will be added here -->
                                    </tbody>
                                </table>
                            </div>
                            <div id="trigger-edit-form-container" class="edit-form-container">
                                <!-- Edit form will be placed here -->
                            </div>
                            <button id="add-trigger" class="add-btn">Add Trigger</button>
                        </div>
                    </div>

                    <!-- Keys Tab -->
                    <div class="tab-content" id="keys-tab">
                        <h3>Key Bindings</h3>
                        <div>
                            <div class="table-container">
                                <table style="width: 100%;">
                                    <thead>
                                        <tr>
                                            <th>Key</th>
                                            <th>Command</th>
                                            <th>Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody id="keys-list">
                                        <!-- Key bindings will be added here -->
                                    </tbody>
                                </table>
                            </div>
                            <div id="edit-form-container" class="edit-form-container">
                                <!-- Edit form will be placed here -->
                            </div>
                            <button id="add-key" class="add-btn">Add Key Binding</button>
                        </div>
                    </div>

                    <!-- Variables Tab -->
                    <div class="tab-content" id="variables-tab">
                        <h3>Variables</h3>
                        <div>
                            <div class="table-container">
                                <table style="width: 100%;">
                                    <thead>
                                        <tr>
                                            <th>Name</th>
                                            <th>Type</th>
                                            <th>Value</th>
                                            <th>Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody id="variables-list">
                                        <!-- Variables will be added here -->
                                    </tbody>
                                </table>
                            </div>
                            <div id="variable-edit-form-container" class="edit-form-container">
                                <!-- Edit form will be placed here -->
                            </div>
                            <button id="add-variable" class="add-btn">Add Variable</button>
                        </div>
                    </div>
                    <!-- Help Tab -->
<div class="tab-content" id="help-tab">
    <h3>Help Documentation</h3>
    <div class="help-content">
        <h4>Basic Commands</h4>
        <p>Type these commands directly into the input field:</p>
        <pre><code class="language-javascript">
// Connect to the server
/connect

// Disconnect from the server
/disconnect
        </code></pre>
        
        <h4>Using Aliases</h4>
        <p>Aliases allow you to create shortcuts for commonly used commands:</p>
        <pre><code class="language-javascript">
// Example aliases in the Settings > Aliases tab:
n = north
s = south
l = look
        </code></pre>
        
        <h4>Using Variables</h4>
        <p>Variables can store values for use in your commands:</p>
        <pre><code class="language-javascript">
// Set a variable (name will be stored in uppercase)
This is not implemented yet.
/var hp = 100

// Use a variable in a command (wrap in $)
say My current HP is $HP$
        </code></pre>
        
        <h4>Creating Triggers</h4>
        <p>Triggers can run commands or JavaScript when matching text appears:</p>
        <pre><code class="language-javascript">
// JavaScript trigger example:
// This trigger will highlight text in gray when you say something
// Match field: ^You say '.+'$
// Type: regex
// Action Type: javascript

const regex = /^You say '(.+)'$/m;
const match = event.cleanMessage.match(regex);

if (match && match[1]) {
    // Find the start position of the quoted text
    const startQuotePos = event.cleanMessage.indexOf("'") + 1;
    
    // Get the length of the quoted text
    const quotedTextLength = match[1].length;
    
    // Apply color to just the quoted text
    mud.applyColor(event, "#A0A0A0", startQuotePos, startQuotePos + quotedTextLength);
    
    // Echo additional text
    mud.echo(\`
### \${match[1]} ###
\`);
}
        </code></pre>
        
        <h4>Available JavaScript APIs</h4>
        <p>In JavaScript triggers, you have access to these objects:</p>
        <pre><code class="language-javascript">
// The mud object provides these methods:
mud.echo(text)             // Display text in the terminal
mud.applyColor(event, color, startIndex, endIndex)  // Colorize text
mud.sendCommand(command)   // Send a command to the MUD

// The event object contains:
event.message         // The original message with ANSI codes
event.cleanMessage    // The message with ANSI codes stripped
event.startIndex      // The start index of the matched text
event.endIndex        // The end index of the matched text

// Additional APIs:
mud.setVariable(name, value, type)  // Set a variable
mud.getVariable(name)              // Get a variable value
mud.createTimer(callback, delay)    // Create a timer (ms)
mud.createInterval(callback, interval)  // Create repeating timer
mud.cancelTimer(id)                // Cancel a timer
mud.cancelInterval(id)             // Cancel an interval
        </code></pre>
    </div>
</div>
                    <div class="modal-footer button-row">
                        <button id="save-settings">Save</button>
                        <button id="cancel-settings">Cancel</button>
                    </div>
                </div>
            </div>
        `,document.body.appendChild(e)}createImportConfirmModal(){let e=document.createElement("div");e.className="modal-overlay",e.id="import-confirm-modal",e.style.display="none",e.innerHTML=`
            <div class="modal-container" style="width: 50vw; height: auto;">
                <div class="modal-content">
                    <div class="modal-header">
                        <h2>Import Settings</h2>
                        <span class="modal-close" id="import-confirm-close">&times;</span>
                    </div>
                    <div style="padding: 20px; color: white;">
                        <p>Importing settings will replace your current configuration. This includes:</p>
                        <ul style="margin-left: 20px; margin-bottom: 20px;">
                            <li>Aliases</li>
                            <li>Key Bindings</li>
                            <li>Triggers</li>
                            <li>Variables</li>
                            <li>Visual Settings</li>
                        </ul>
                        <p>Are you sure you want to continue?</p>
                    </div>
                    <div class="modal-footer">
                        <button id="confirm-import">Yes, Import</button>
                        <button id="cancel-import" style="background-color: #555;">Cancel</button>
                    </div>
                </div>
            </div>
        `,document.body.appendChild(e);let i=document.createElement("input");i.type="file",i.id="settings-file-input",i.accept=".json",i.style.display="none",document.body.appendChild(i)}createProfileConfirmModal(){let e=document.createElement("div");e.className="modal-overlay",e.id="profile-confirm-modal",e.style.display="none",e.innerHTML=`
            <div class="modal-container" style="width: 50vw; height: auto;">
                <div class="modal-content">
                    <div class="modal-header">
                        <h2>Delete Profile</h2>
                        <span class="modal-close" id="profile-confirm-close">&times;</span>
                    </div>
                    <div style="padding: 20px; color: white;">
                        <p>Are you sure you want to delete this profile?</p>
                        <p>This action cannot be undone.</p>
                    </div>
                    <div class="modal-footer">
                        <button id="confirm-delete-profile" style="background-color: #dc3545;">Delete</button>
                        <button id="cancel-delete-profile" style="background-color: #555;">Cancel</button>
                    </div>
                </div>
            </div>
        `,document.body.appendChild(e)}initializeUIElements(){this.settingsModal=document.getElementById("settings-modal"),this.modalTitle=document.getElementById("modal-title"),this.closeBtn=document.querySelector(".modal-close"),this.cancelBtn=document.getElementById("cancel-settings"),this.saveBtn=document.getElementById("save-settings"),this.menuSettings=document.getElementById("menu-settings"),this.menuAliases=document.getElementById("menu-aliases"),this.menuTriggers=document.getElementById("menu-triggers"),this.menuKeys=document.getElementById("menu-keys"),this.menuVariables=document.getElementById("menu-variables"),this.menuHelp=document.getElementById("menu-help"),this.sidebarItems=document.querySelectorAll(".sidebar-item"),this.tabContents=document.querySelectorAll(".tab-content"),this.fontSizeInput=document.getElementById("font-size"),this.bgColorInput=document.getElementById("bg-color"),this.textColorInput=document.getElementById("text-color"),this.bgColorValue=document.getElementById("bg-color-value"),this.textColorValue=document.getElementById("text-color-value"),this.resetBtn=document.getElementById("reset-settings"),this.updateSettingsUI()}initializeEventListeners(){this.menuSettings&&this.menuSettings.addEventListener("click",()=>this.openModal("settings")),this.menuAliases&&this.menuAliases.addEventListener("click",()=>this.openModal("aliases")),this.menuTriggers&&this.menuTriggers.addEventListener("click",()=>this.openModal("triggers")),this.menuKeys&&this.menuKeys.addEventListener("click",()=>this.openModal("keys")),this.menuVariables&&this.menuVariables.addEventListener("click",()=>this.openModal("variables")),this.menuHelp&&this.menuHelp.addEventListener("click",()=>this.openModal("help")),this.closeBtn&&this.closeBtn.addEventListener("click",()=>this.closeModal()),this.cancelBtn&&this.cancelBtn.addEventListener("click",()=>this.closeModal()),this.settingsModal&&this.settingsModal.addEventListener("click",d=>{d.target===this.settingsModal&&this.closeModal()}),this.sidebarItems&&this.sidebarItems.forEach(d=>{d.addEventListener("click",()=>{let p=d.getAttribute("data-tab");p&&this.switchTab(p)})}),this.bgColorInput&&this.bgColorValue&&this.bgColorInput.addEventListener("input",()=>{this.bgColorValue&&(this.bgColorValue.textContent=this.bgColorInput.value)}),this.textColorInput&&this.textColorValue&&this.textColorInput.addEventListener("input",()=>{this.textColorValue&&(this.textColorValue.textContent=this.textColorInput.value)}),this.resetBtn&&this.resetBtn.addEventListener("click",()=>this.resetSettings()),this.saveBtn&&this.saveBtn.addEventListener("click",()=>this.saveSettings());let e=document.getElementById("connect-button"),i=document.getElementById("disconnect-button");e&&e.addEventListener("click",()=>{this.app.sendCommand("/connect"),this.focusInput()}),i&&i.addEventListener("click",()=>{this.app.sendCommand("/disconnect"),this.focusInput()});let t=document.getElementById("export-settings"),l=document.getElementById("import-settings"),a=document.getElementById("settings-file-input"),n=document.getElementById("import-confirm-modal"),s=document.getElementById("import-confirm-close"),o=document.getElementById("confirm-import"),r=document.getElementById("cancel-import");t&&t.addEventListener("click",()=>this.exportSettings()),l&&a&&(l.addEventListener("click",()=>{a.click()}),a.addEventListener("change",d=>{a.files&&a.files.length>0&&n&&(n.style.display="block",this.app.setModalOpen(!0))})),s&&n&&s.addEventListener("click",()=>{n.style.display="none",a&&(a.value=""),this.app.setModalOpen(!1)}),r&&n&&r.addEventListener("click",()=>{n.style.display="none",a&&(a.value=""),this.app.setModalOpen(!1)}),o&&n&&a&&o.addEventListener("click",()=>{this.importSettings(a.files?.[0]),n.style.display="none",this.app.setModalOpen(!1)}),n&&n.addEventListener("click",d=>{d.target===n&&(n.style.display="none",a&&(a.value=""),this.app.setModalOpen(!1))})}initializeStyles(){let e=document.createElement("style");e.textContent=`
            @keyframes fadeOut {
                from { opacity: 1; }
                to { opacity: 0; }
            }
            
            .key-capture-overlay {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background-color: rgba(0, 0, 0, 0.7);
                display: flex;
                justify-content: center;
                align-items: center;
                z-index: 2000;
            }
            
            .key-capture-dialog {
                background-color: #222;
                border: 1px solid #444;
                border-radius: 4px;
                padding: 20px;
                width: 400px;
                max-width: 90%;
            }
            
            .key-capture-content {
                text-align: center;
            }
            
            .key-display {
                margin: 20px 0;
                padding: 10px;
                background-color: #333;
                border: 1px solid #555;
                border-radius: 4px;
                font-family: monospace;
                font-size: 18px;
                color: white;
            }
            
            .pattern-test-container {
                margin-top: 20px;
                padding: 15px;
                background-color: #222;
                border: 1px solid #444;
                border-radius: 4px;
            }
            
            .pattern-test-result {
                margin-top: 10px;
                padding: 10px;
                border-radius: 4px;
            }
            
            .pattern-test-result.success {
                background-color: rgba(40, 167, 69, 0.3);
                border: 1px solid #28a745;
            }
            
            .pattern-test-result.failure {
                background-color: rgba(220, 53, 69, 0.3);
                border: 1px solid #dc3545;
            }
            /* Help tab styles */
            .help-content {
                padding: 15px;
                overflow-y: auto;
                max-height: 70vh;
                color: #eee;
                font-size: 14px;
            }
            
            .help-content h4 {
                margin-top: 25px;
                margin-bottom: 10px;
                border-bottom: 1px solid #444;
                padding-bottom: 8px;
                color: #ddd;
            }
            
            .help-content p {
                margin-bottom: 10px;
                line-height: 1.5;
            }
            
            .help-content pre {
                background-color: #1E1E1E;
                border: 1px solid #333;
                border-radius: 4px;
                padding: 15px;
                margin: 10px 0;
                overflow-x: auto;
            }
            
            .help-content code {
                font-family: Consolas, Monaco, 'Andale Mono', 'Ubuntu Mono', monospace;
                font-size: 13px;
                color: #dcdcdc;
            }
            
            /* highlight.js theme overrides */
            .hljs-comment, .hljs-quote {
                color: #608b4e;
                font-style: italic;
            }
            
            .hljs-keyword, .hljs-selector-tag {
                color: #569cd6;
            }
            
            .hljs-string, .hljs-attribute, .hljs-addition {
                color: #ce9178;
            }
            
            .hljs-number, .hljs-literal {
                color: #b5cea8;
            }
            
            .hljs-type, .hljs-built_in {
                color: #4ec9b0;
            }
        `,document.head.appendChild(e);let i=document.createElement("link");i.rel="stylesheet",i.href="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.7.0/styles/vs2015.min.css",document.head.appendChild(i);let t=document.createElement("script");t.src="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.7.0/highlight.min.js",document.head.appendChild(t),t.onload=()=>{typeof window.hljs<"u"&&window.hljs.highlightAll()}}openModal(e){this.app.setModalOpen(!0),this.loadSavedSettings(),this.settingsModal&&(this.settingsModal.style.display="block"),this.switchTab(e),this.modalTitle&&(this.modalTitle.textContent=e.charAt(0).toUpperCase()+e.slice(1))}closeModal(){this.settingsModal&&(this.settingsModal.style.display="none"),this.app.setModalOpen(!1),this.focusInput()}switchTab(e){if(!(!this.sidebarItems||!this.tabContents))switch(e==="settings"&&this.loadSavedSettings(),this.sidebarItems.forEach(i=>{i.classList.remove("active"),i.getAttribute("data-tab")===e&&i.classList.add("active")}),this.tabContents.forEach(i=>{i.classList.remove("active"),i.id===`${e}-tab`&&i.classList.add("active")}),this.modalTitle&&(this.modalTitle.textContent=e.charAt(0).toUpperCase()+e.slice(1)),e){case"aliases":this.aliasesUI.loadAliases();break;case"triggers":this.triggersUI.loadTriggers();break;case"keys":this.keyBindingsUI.loadKeyBindings();break;case"variables":this.variablesUI.loadVariables();break;case"help":typeof window.hljs<"u"&&setTimeout(()=>{window.hljs.highlightAll()},0);break}}loadSavedSettings(){!this.fontSizeInput||!this.bgColorInput||!this.textColorInput||!this.bgColorValue||!this.textColorValue||(this.fontSizeInput.value=this.app.settings.fontSize.toString(),this.bgColorInput.value=this.app.settings.backgroundColor,this.bgColorValue.textContent=this.app.settings.backgroundColor,this.textColorInput.value=this.app.settings.foregroundColor,this.textColorValue.textContent=this.app.settings.foregroundColor,this.profilesUI.updateUI(),this.aliasesUI.updateUI(),this.keyBindingsUI.updateUI(),this.triggersUI.updateUI(),this.variablesUI.updateUI())}resetSettings(){this.fontSizeInput&&(this.fontSizeInput.value="14"),this.bgColorInput&&this.bgColorValue&&(this.bgColorInput.value="#000000",this.bgColorValue.textContent="#000000"),this.textColorInput&&this.textColorValue&&(this.textColorInput.value="#FFFFFF",this.textColorValue.textContent="#FFFFFF")}saveSettings(){this.fontSizeInput&&(this.app.settings.fontSize=parseInt(this.fontSizeInput.value)),this.bgColorInput&&(this.app.settings.backgroundColor=this.bgColorInput.value),this.textColorInput&&(this.app.settings.foregroundColor=this.textColorInput.value),this.app.saveSettings(),this.app.applySettings(),this.closeModal()}exportSettings(){let e=this.app.exportSettings(),i=new Blob([e],{type:"application/json"}),t=URL.createObjectURL(i),l=new Date,n=`mud-settings-${`${l.getFullYear()}-${String(l.getMonth()+1).padStart(2,"0")}-${String(l.getDate()).padStart(2,"0")}`}.json`,s=document.createElement("a");s.href=t,s.download=n,document.body.appendChild(s),s.click(),setTimeout(()=>{document.body.removeChild(s),URL.revokeObjectURL(t)},100)}importSettings(e){if(!e){this.app.showNotification("No file selected",!1);return}let i=new FileReader;i.onload=t=>{try{if(typeof t.target?.result!="string")throw new Error("Invalid file format");this.app.importSettings(t.target.result)?(this.app.showNotification("Settings imported successfully!",!0),this.loadSavedSettings()):this.app.showNotification("Error importing settings",!1)}catch(a){this.app.showNotification(`Error reading file: ${a}`,!1)}let l=document.getElementById("settings-file-input");l&&(l.value="")},i.onerror=()=>{this.app.showNotification("Error reading file",!1);let t=document.getElementById("settings-file-input");t&&(t.value="")},i.readAsText(e)}focusInput(){let e=document.getElementById("input");e&&e.select()}updateSettingsUI(){!this.fontSizeInput||!this.bgColorInput||!this.textColorInput||!this.bgColorValue||!this.textColorValue||(this.fontSizeInput.value=this.app.settings.fontSize.toString(),this.bgColorInput.value=this.app.settings.backgroundColor,this.bgColorValue.textContent=this.app.settings.backgroundColor,this.textColorInput.value=this.app.settings.foregroundColor,this.textColorValue.textContent=this.app.settings.foregroundColor,this.profilesUI.updateUI())}updateUI(){this.updateSettingsUI(),this.aliasesUI.updateUI(),this.triggersUI.updateUI(),this.keyBindingsUI.updateUI(),this.variablesUI.updateUI(),this.profilesUI.updateUI()}};export{D as AppSettingsUI};
//# sourceMappingURL=AppSettingsUI.js.map
